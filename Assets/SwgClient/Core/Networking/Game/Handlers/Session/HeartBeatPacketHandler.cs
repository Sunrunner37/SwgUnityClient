using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Session;

namespace Swg.Client.Core.Networking.Game.Handlers.Session
{
    // Core3/MMOCoreORB/src/server/zone/packets/HeartBeat.h:12 (HeartBeat).
    // Core3/MMOCoreORB/src/server/zone/packets/LinkDeadMessage.h:10 (LinkDeadMessage).
    [Preserve]
    internal sealed class HeartBeatPacketHandler : GamePacketHandler<HeartBeatPacket>
    {
        public override uint Opcode => HeartBeatPacket.Opcode;

        [Preserve]
        public HeartBeatPacketHandler() : base(HeartBeatPacket.ReadFrom)
        {
        }

        protected override void Handle(HeartBeatPacket packet)
        {
            throw new NotImplementedException($"HeartBeat (opcode 0x{Opcode:X8})");
        }
    }
}
