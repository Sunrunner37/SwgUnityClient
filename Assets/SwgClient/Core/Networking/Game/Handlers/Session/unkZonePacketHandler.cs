using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Session;

namespace Swg.Client.Core.Networking.Game.Handlers.Session
{
    // Core3/MMOCoreORB/src/server/zone/packets/zone/unkZone.h:14 (unkZone).
    [Preserve]
    internal sealed class unkZonePacketHandler : GamePacketHandler<unkZonePacket>
    {
        public override uint Opcode => unkZonePacket.Opcode;

        [Preserve]
        public unkZonePacketHandler() : base(unkZonePacket.ReadFrom)
        {
        }

        protected override void Handle(unkZonePacket packet)
        {
            throw new NotImplementedException($"unkZone (opcode 0x{Opcode:X8})");
        }
    }
}
