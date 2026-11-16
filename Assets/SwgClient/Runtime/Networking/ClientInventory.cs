using Swg.Client.Core.Characters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Swg.Client.Core;
using Swg.Client.Core.World;
using Swg.Client.Core.World.SceneObjects;
using Swg.Client.Core.Networking.Game.Packets.Scene;
using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;

namespace Swg.Client.Runtime.Networking
{
    /// <summary>Inventory contents are the replicated containment graph; no second item store is maintained.</summary>
    public sealed class ClientInventory : IDisposable
    {
        readonly WorldState state;
        readonly CurrentPlayer currentPlayer;
        readonly Action<SwgId> requestContents;
        readonly SynchronizationContext context;
        bool disposed;
        public SwgId ContainerSwgId { get; private set; }
        public IReadOnlyList<TangibleSceneObject> Items { get; private set; } = Array.Empty<TangibleSceneObject>();
        public int Capacity { get; private set; }
        public int UsedCapacity => state.SceneObjects.ChildrenOf(ContainerSwgId).OfType<TangibleSceneObject>().Sum(item => Math.Max(1, item.Volume));
        public CreatureSceneObject Player => currentPlayer.Creature;
        public event Action Changed;
        public event Action<SwgId> ContainerOpened;
        public IReadOnlyList<TangibleSceneObject> Contents(SwgId container) => state.SceneObjects.ChildrenOf(container).OfType<TangibleSceneObject>().OrderBy(item => item.SwgId.Value).ToArray();
        public int Used(SwgId container) => Contents(container).Sum(item => Math.Max(1, item.Volume));
        public int Limit(SwgId container) => state.SceneObjects.TryGet(container, out var item) ? state.Templates.ContainerCapacity(item.TemplatePath) : 0;
        public ClientInventory(WorldState state, Action<SwgId> requestContents, CurrentPlayer currentPlayer)
        {
            this.currentPlayer = currentPlayer;
            this.state = state;
            this.requestContents = requestContents;
            context = SynchronizationContext.Current ?? throw new InvalidOperationException("Create inventory on Unity's main thread.");
            state.SceneObjects.ObjectChanged += ObjectChanged;
            state.SceneObjects.Removed += Removed;
            Refresh();
        }
        public void Open()
        {
            Refresh();
            if (!ContainerSwgId.IsNone)
                requestContents(ContainerSwgId);
        }
        public static bool CanEquip(TangibleSceneObject item) => item.TemplatePath != null
            && (item.TemplatePath.StartsWith("object/tangible/wearables/", StringComparison.Ordinal)
                || item.TemplatePath.StartsWith("object/weapon/", StringComparison.Ordinal));
        public void Activate(SwgId swgId)
        {
            if (state.SceneObjects.TryGet<TangibleSceneObject>(swgId, out var item)
                && !((ObjectTypeId)state.Templates.GameObjectType(item.TemplatePath)).IsContainer() && CanEquip(item))
            {
                bool equipped = IsEquipped(item);
                var destination = equipped ? ContainerSwgId : currentPlayer.SwgId;
                GameServices.Resolve<ClientCommands>().Enqueue("transferItemMisc", destination + (equipped ? " -1 0 0 0" : " 4 0 0 0"), swgId);
            }
            else
                GameServices.Resolve<ClientObjectInteractions>().ActivateDefault(swgId);
        }
        public bool IsEquipped(TangibleSceneObject item) => item.ParentSwgId == currentPlayer.SwgId;
        void ObjectChanged(SceneObject sceneObject, SceneObjectChange changes)
        {
            if ((changes & (SceneObjectChange.Appearance | SceneObjectChange.Containment | SceneObjectChange.Inventory | SceneObjectChange.Replaced)) == 0)
                return;
            if (sceneObject.SwgId == currentPlayer.SwgId || sceneObject.ParentSwgId == currentPlayer.SwgId || sceneObject.ParentSwgId == ContainerSwgId || Items.Any(item => item.SwgId == sceneObject.SwgId))
                Refresh();
        }
        void Removed(SceneObject sceneObject)
        {
            if (sceneObject.SwgId == ContainerSwgId || Items.Any(item => item.SwgId == sceneObject.SwgId))
                Refresh();
        }
        void Refresh()
        {
            var children = state.SceneObjects.ChildrenOf(currentPlayer.SwgId);
            var container = children.FirstOrDefault(item => item.TemplatePath?.StartsWith("object/tangible/inventory/", StringComparison.Ordinal) == true);
            ContainerSwgId = container?.SwgId ?? default;
            Capacity = container == null ? 0 : state.Templates.ContainerCapacity(container.TemplatePath);
            Items = state.SceneObjects.ChildrenOf(ContainerSwgId).OfType<TangibleSceneObject>()
                .Concat(children.OfType<TangibleSceneObject>().Where(CanEquip))
                .OrderBy(item => item.SwgId.Value).ToArray();
            Changed?.Invoke();
        }
        public void Receive(ClientOpenContainerPacket packet) => context.Post(_ =>
        {
            if (disposed) return;
            if (packet.SwgId == ContainerSwgId.Value) Refresh();
            else if (state.SceneObjects.TryGet(packet.SwgId, out var container)) ContainerOpened?.Invoke(packet.SwgId);
        }, null);
        public void Dispose()
        {
            disposed = true;
            state.SceneObjects.ObjectChanged -= ObjectChanged;
            state.SceneObjects.Removed -= Removed;
        }
    }
}
