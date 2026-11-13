using System;
using System.Collections.Generic;
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
        public const byte ExamineAction = 7; // Core3 RadialOptions / native radial_menu.iff.
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

        public ClientObjectInteractions(WorldState state, Action<ObjectMenuRequestPacket> sendMenu, Action<SwgId> requestAttributes)
        {
            this.state = state;
            this.sendMenu = sendMenu;
            this.requestAttributes = requestAttributes;
            context = SynchronizationContext.Current ?? throw new InvalidOperationException("Create object interactions on Unity's main thread.");
        }

        public void RequestMenu(SwgId swgId)
        {
            MenuTargetSwgId = swgId;
            var defaults = new[] { new RadialMenuEntry(1, 0, ExamineAction, 1, "@ui_radial:examine") };
            Menu = defaults;
            MenuChanged?.Invoke();
            sendMenu(new ObjectMenuRequestPacket(state.PlayerSwgId, swgId, defaults, unchecked(++counter)));
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
