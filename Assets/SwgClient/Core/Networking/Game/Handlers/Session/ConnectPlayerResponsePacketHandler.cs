using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Session;

namespace Swg.Client.Core.Networking.Game.Handlers.Session
{
    // Core3/MMOCoreORB/src/server/zone/packets/zone/ConnectPlayerResponseMessage.h:14 (ConnectPlayerResponseMessage).
    [Preserve]
    internal sealed class ConnectPlayerResponsePacketHandler : GamePacketHandler<ConnectPlayerResponsePacket>
    {
        public override uint Opcode => ConnectPlayerResponsePacket.Opcode;

        [Preserve]
        public ConnectPlayerResponsePacketHandler() : base(ConnectPlayerResponsePacket.ReadFrom)
        {
        }

        protected override void Handle(ConnectPlayerResponsePacket packet)
        {
            throw new NotImplementedException($"ConnectPlayerResponseMessage (opcode 0x{Opcode:X8})");
        }
    }
}
