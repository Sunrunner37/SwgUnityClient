using Swg.Client.Core.Characters;
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
        readonly ObjectMenuDefaults menuDefaults;
        readonly CurrentPlayer currentPlayer;
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
        public event Action<SwgId> ExamineRequested;
        public event Action<SwgId> DestroyRequested;
        public event Action<SwgId> AttributesChanged;

        public ClientObjectInteractions(WorldState state, Action<ObjectMenuRequestPacket> sendMenu, Action<SwgId> requestAttributes, Action<ObjectMenuSelectPacket> selectMenu, Action<string, string, SwgId> enqueueCommand, CurrentPlayer currentPlayer, ObjectMenuDefaults menuDefaults)
        {
            this.currentPlayer = currentPlayer;
            this.menuDefaults = menuDefaults;
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
            var defaults = menuDefaults.Create(sceneObject);
            Menu = defaults;
            MenuChanged?.Invoke();
            sendMenu(new ObjectMenuRequestPacket(currentPlayer.SwgId, swgId, defaults, unchecked(++counter)));
        }

        internal ObjectTypeId ObjectType(SceneObject sceneObject) => menuDefaults.ObjectType(sceneObject);

        public bool IsActiveControlDevice(SwgId swgId) => state.SceneObjects.TryGet<IntangibleSceneObject>(swgId, out var device) && device.IsActive && ObjectType(device).IsControlDevice();

        public bool HasDefaultAction(SwgId swgId) => state.SceneObjects.TryGet(swgId, out var sceneObject) && menuDefaults.TryGetDefaultAction(sceneObject, out _);

        public void ActivateDefault(SwgId swgId)
        {
            if (!state.SceneObjects.TryGet(swgId, out var sceneObject)) return;
            if (ObjectType(sceneObject).IsVehicle())
                SelectMenuAction(swgId, ObjectMenuAction.VehicleEnterExit);
            else if (menuDefaults.TryGetDefaultAction(sceneObject, out var action))
                SelectMenuAction(swgId, action);
            else
                ExamineRequested?.Invoke(swgId);
        }
        public void SelectMenuAction(SwgId swgId, ObjectMenuAction action) => SelectMenuAction(swgId, action.Id, action.Callback);

        public void SelectMenuAction(SwgId swgId, byte radialId, byte callback = 3)
        {
            state.SceneObjects.TryGet(swgId, out var sceneObject);
            if (radialId == ObjectMenuAction.Destroy.Id)
            {
                if (menuDefaults.CanDestroy(sceneObject)) DestroyRequested?.Invoke(swgId);
            }
            else if (radialId == ObjectMenuAction.Open.Id)
                enqueueCommand("openContainer", "", swgId);
            else if (radialId == ObjectMenuAction.ConverseStart.Id)
                GameServices.Resolve<ClientConversation>().Start(swgId);
            else if (radialId == ObjectMenuAction.ConverseStop.Id)
                GameServices.Resolve<ClientConversation>().Stop();
            else if (radialId == ObjectMenuAction.MissionList.Id && ObjectType(sceneObject).IsMissionTerminal())
                GameServices.Resolve<ClientMissions>().Open(swgId);
            else if (sceneObject is MissionSceneObject && radialId == ObjectMenuAction.MissionDetails.Id)
                GameServices.Resolve<ClientMissions>().ShowDetails(swgId);
            else if (sceneObject is MissionSceneObject && radialId == ObjectMenuAction.MissionAbort.Id)
                GameServices.Resolve<ClientMissions>().RequestAbort(swgId);
            else if (radialId == ObjectMenuAction.Use.Id && ObjectType(sceneObject).IsCrafting())
                enqueueCommand("requestCraftingSession", "", swgId);
            else if (callback == 1 && radialId == ObjectMenuAction.VehicleEnterExit.Id)
            {
                state.SceneObjects.TryGet<CreatureSceneObject>(currentPlayer.SwgId, out var player);
                enqueueCommand(player?.Mount != null ? "dismount" : "mount", "", swgId);
            }
            else
                selectMenu(new ObjectMenuSelectPacket(swgId, radialId));
        }

        public void Destroy(SwgId swgId)
        {
            if (state.SceneObjects.TryGet(swgId, out var sceneObject) && menuDefaults.CanDestroy(sceneObject))
                enqueueCommand("serverDestroyObject", "", swgId);
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
