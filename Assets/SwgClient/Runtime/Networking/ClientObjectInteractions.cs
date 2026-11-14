using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;
using System;
using System.Collections.Generic;
using Swg.Client.Core.World.SceneObjects;
using System.Threading;
using Swg.Client.Core;
using Swg.Client.Core.Networking.Game.Packets.Data;
using Swg.Client.Core.Networking.Game.Packets.Ui;
using Swg.Client.Core.Networking.Game.Packets.Scene;
using Swg.Client.Core.World;

namespace Swg.Client.Runtime.Networking
{
    /// <summary>Game-owned menu and examine requests. Network replies are delivered to views on the main thread.</summary>
    public sealed class ClientObjectInteractions : IDisposable
    {
        readonly Action<ObjectMenuSelectPacket> selectMenu;
        readonly Action<string, string, SwgId> enqueueCommand;
        readonly WorldState state;
        readonly Action<ObjectMenuRequestPacket> sendMenu;
        readonly Action<SwgId> requestAttributes;
        readonly SynchronizationContext context;
        byte counter;
        bool disposed; // Posted main-thread replies can outlive the game scope.
        public SwgId MenuTargetSwgId { get; private set; }
        public IReadOnlyList<RadialMenuEntry> Menu { get; private set; } = Array.Empty<RadialMenuEntry>();
        readonly Dictionary<SwgId, IReadOnlyList<AttributeValue>> attributes = new Dictionary<SwgId, IReadOnlyList<AttributeValue>>();
        public IReadOnlyList<AttributeValue> GetAttributes(SwgId swgId) => attributes.TryGetValue(swgId, out var values) ? values : Array.Empty<AttributeValue>();
        public event Action MenuChanged;
        public event Action<SwgId> AttributesChanged;

        public ClientObjectInteractions(WorldState state, Action<ObjectMenuRequestPacket> sendMenu, Action<SwgId> requestAttributes, Action<ObjectMenuSelectPacket> selectMenu, Action<string, string, SwgId> enqueueCommand)
        {
            this.state = state;
            this.selectMenu = selectMenu;
            this.enqueueCommand = enqueueCommand;
            this.sendMenu = sendMenu;
            this.requestAttributes = requestAttributes;
            context = SynchronizationContext.Current ?? throw new InvalidOperationException("Create object interactions on Unity's main thread.");
        }

        public void RequestMenu(SwgId swgId)
        {
            MenuTargetSwgId = swgId;
            state.SceneObjects.TryGet(swgId, out var sceneObject);
            var defaults = ObjectMenuDefaults.Create(sceneObject, ObjectType(sceneObject));
            Menu = defaults;
            MenuChanged?.Invoke();
            sendMenu(new ObjectMenuRequestPacket(state.PlayerSwgId, swgId, defaults, unchecked(++counter)));
        }

        int ObjectType(SceneObject sceneObject) => sceneObject == null ? 0 : state.Templates.GameObjectType(sceneObject.TemplatePath);

        public bool IsActiveControlDevice(SwgId swgId) => state.SceneObjects.TryGet<IntangibleSceneObject>(swgId, out var device) && device.IsActive && ObjectMenuDefaults.IsControlDevice(ObjectType(device));

        public bool HasDefaultAction(SwgId swgId) => state.SceneObjects.TryGet(swgId, out var sceneObject) && ObjectMenuDefaults.TryGetDefaultAction(sceneObject, ObjectType(sceneObject), out _);

        public void ActivateDefault(SwgId swgId)
        {
            if (state.SceneObjects.TryGet(swgId, out var sceneObject) && ObjectMenuDefaults.TryGetDefaultAction(sceneObject, ObjectType(sceneObject), out byte action))
                SelectMenuAction(swgId, action);
        }
        public void SelectMenuAction(SwgId swgId, byte radialId, byte callback = 3)
        {
            if (callback == 1 && radialId == ObjectMenuDefaults.VehicleEnterExitAction)
            {
                state.SceneObjects.TryGet<CreatureSceneObject>(state.PlayerSwgId, out var player);
                enqueueCommand(player?.Mount != null ? "dismount" : "mount", "", swgId);
            }
            else
                selectMenu(new ObjectMenuSelectPacket(swgId, radialId));
        }

        public void RequestExamine(SwgId swgId) => requestAttributes(swgId);

        public void Receive(ObjectMenuResponsePacket packet) => context.Post(_ =>
        {
            if (disposed || packet.TargetSwgId != MenuTargetSwgId.Value || packet.Counter != counter)
                return;
            Menu = packet.Items;
            MenuChanged?.Invoke();
        }, null);

        public void Receive(AttributeListPacket packet) => context.Post(_ =>
        {
            if (disposed)
                return;
            attributes[packet.SwgId] = packet.Attributes;
            AttributesChanged?.Invoke(packet.SwgId);
        }, null);

        public void Dispose() => disposed = true;
    }
}
