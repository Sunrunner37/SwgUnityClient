using System;
using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;
using Swg.Client.Core;
using Swg.Client.Core.Networking.Game.Packets.Targeting;
using Swg.Client.Core.World;
using Swg.Client.Core.World.SceneObjects;

namespace Swg.Client.Runtime.World.SceneObjects
{
    /// <summary>Game-lifetime selection and synchronization with the player's replicated target.</summary>
    public sealed class ClientTargeting : IDisposable
    {
        readonly WorldState state;
        readonly SceneObjectRegistry registry;
        readonly Action<TargetUpdatePacket> send;
        SwgId playerSwgId;
        public SwgId SelectedSwgId { get; private set; }
        public SwgId HoveredSwgId { get; private set; }

        public ClientTargeting(WorldState state, Action<TargetUpdatePacket> send)
        {
            this.state = state;
            this.send = send;
            registry = state.SceneObjects;
            registry.Removed += OnRemoved;
            state.SceneStarted += BindPlayer;
            BindPlayer();
        }

        public void Select(SwgId swgId)
        {
            if (!swgId.IsNone && !registry.TryGet<SpatialSceneObject>(swgId, out _))
                return;
            if (!SetSelection(swgId))
                return;
            if (registry.TryGet<CreatureSceneObject>(playerSwgId, out var player))
            {
                player.TargetSwgId = swgId;
                registry.NotifyChanged(player, SceneObjectChange.Target);
            }
            send(new TargetUpdatePacket(playerSwgId, swgId));
        }

        bool SetSelection(SwgId swgId)
        {
            if (SelectedSwgId == swgId)
                return false;
            SelectedSwgId = swgId;
            return true;
        }

        void BindPlayer()
        {
            registry.Unsubscribe(playerSwgId, OnPlayerChanged);
            playerSwgId = state.PlayerSwgId;
            SetSelection(default);
            if (!playerSwgId.IsNone)
            {
                registry.Subscribe(playerSwgId, OnPlayerChanged);
                OnPlayerChanged(SceneObjectChange.Target);
            }
        }

        void OnPlayerChanged(SceneObjectChange changes)
        {
            if ((changes & (SceneObjectChange.Target | SceneObjectChange.Replaced)) != 0 && registry.TryGet<CreatureSceneObject>(playerSwgId, out var player))
                SetSelection(player.TargetSwgId); // Replicated changes do not enqueue another target request.
        }

        public void Hover(SwgId swgId) => HoveredSwgId = swgId;

        void OnRemoved(SceneObject sceneObject)
        {
            if (HoveredSwgId == sceneObject.SwgId)
                Hover(default);
            if (SelectedSwgId == sceneObject.SwgId)
                Select(default);
        }

        public void Dispose()
        {
            registry.Removed -= OnRemoved;
            state.SceneStarted -= BindPlayer;
            registry.Unsubscribe(playerSwgId, OnPlayerChanged);
        }
    }
}
