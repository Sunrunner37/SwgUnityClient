using UnityEngine;
using Autofac;
using System.Collections.Generic;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets;

namespace Swg.Client.Core.Networking.Game.Handlers
{
    [Preserve]
    internal sealed class ObjectControllerPacketHandler : GamePacketHandler<ObjectControllerPacket>
    {
        readonly Dictionary<uint, IObjectControllerHandler> controllers;
        public override uint Opcode => ObjectControllerPacket.Opcode;

        [Preserve]
        public ObjectControllerPacketHandler(ILifetimeScope services) : base(ObjectControllerPacket.ReadFrom)
        {
            controllers = PacketHandlerDiscovery.Map(PacketHandlerDiscovery.Create<IObjectControllerHandler>(services), handler => handler.ControllerType);
        }

        protected override void Handle(ObjectControllerPacket packet)
        {
            if (controllers.TryGetValue(packet.ControllerType, out var handler))
                handler.Handle(packet);
            else
                Debug.LogWarning("Unsupported object controller 0x" + packet.ControllerType.ToString("X"));
        }
    }
}
