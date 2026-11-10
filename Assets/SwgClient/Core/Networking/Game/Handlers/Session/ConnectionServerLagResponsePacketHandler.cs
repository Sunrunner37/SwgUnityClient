using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Session;

namespace Swg.Client.Core.Networking.Game.Handlers.Session
{
    // Core3/MMOCoreORB/src/server/zone/packets/zone/LagReport.h:23 (ConnectionServerLagResponse).
    [Preserve]
    internal sealed class ConnectionServerLagResponsePacketHandler : GamePacketHandler<ConnectionServerLagResponsePacket>
    {
        public override uint Opcode => ConnectionServerLagResponsePacket.Opcode;

        [Preserve]
        public ConnectionServerLagResponsePacketHandler() : base(ConnectionServerLagResponsePacket.ReadFrom)
        {
        }

        protected override void Handle(ConnectionServerLagResponsePacket packet)
        {
            throw new NotImplementedException($"ConnectionServerLagResponse (opcode 0x{Opcode:X8})");
        }
    }
}
