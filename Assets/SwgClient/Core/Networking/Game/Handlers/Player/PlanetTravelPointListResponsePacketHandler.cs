using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Player;

namespace Swg.Client.Core.Networking.Game.Handlers.Player
{
    // Core3/MMOCoreORB/src/server/zone/packets/player/PlanetTravelPointListResponse.h:35 (PlanetTravelPointListResponse).
    [Preserve]
    internal sealed class PlanetTravelPointListResponsePacketHandler : GamePacketHandler<PlanetTravelPointListResponsePacket>
    {
        public override uint Opcode => PlanetTravelPointListResponsePacket.Opcode;

        [Preserve]
        public PlanetTravelPointListResponsePacketHandler() : base(PlanetTravelPointListResponsePacket.ReadFrom)
        {
        }

        protected override void Handle(PlanetTravelPointListResponsePacket packet)
        {
            throw new NotImplementedException($"PlanetTravelPointListResponse (opcode 0x{Opcode:X8})");
        }
    }
}
