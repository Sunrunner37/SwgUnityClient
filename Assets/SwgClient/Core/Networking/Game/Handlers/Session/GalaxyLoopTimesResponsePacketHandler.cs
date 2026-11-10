using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Session;

namespace Swg.Client.Core.Networking.Game.Handlers.Session
{
    // Core3/MMOCoreORB/src/server/zone/packets/zone/GalaxyLoopTimes.h:14 (GalaxyLoopTimesResponse).
    [Preserve]
    internal sealed class GalaxyLoopTimesResponsePacketHandler : GamePacketHandler<GalaxyLoopTimesResponsePacket>
    {
        public override uint Opcode => GalaxyLoopTimesResponsePacket.Opcode;

        [Preserve]
        public GalaxyLoopTimesResponsePacketHandler() : base(GalaxyLoopTimesResponsePacket.ReadFrom)
        {
        }

        protected override void Handle(GalaxyLoopTimesResponsePacket packet)
        {
            throw new NotImplementedException($"GalaxyLoopTimesResponse (opcode 0x{Opcode:X8})");
        }
    }
}
