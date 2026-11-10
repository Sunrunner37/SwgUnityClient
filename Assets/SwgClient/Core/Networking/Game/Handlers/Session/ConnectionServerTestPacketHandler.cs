using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Session;

namespace Swg.Client.Core.Networking.Game.Handlers.Session
{
    // Core3/MMOCoreORB/src/server/zone/packets/zone/ConnectionServerTestMessage.h:13 (ConnectionServerTestMessage).
    [Preserve]
    internal sealed class ConnectionServerTestPacketHandler : GamePacketHandler<ConnectionServerTestPacket>
    {
        public override uint Opcode => ConnectionServerTestPacket.Opcode;

        [Preserve]
        public ConnectionServerTestPacketHandler() : base(ConnectionServerTestPacket.ReadFrom)
        {
        }

        protected override void Handle(ConnectionServerTestPacket packet)
        {
            throw new NotImplementedException($"ConnectionServerTestMessage (opcode 0x{Opcode:X8})");
        }
    }
}
