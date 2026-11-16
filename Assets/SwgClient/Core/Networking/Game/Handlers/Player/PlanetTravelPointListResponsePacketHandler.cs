using Swg.Client.Runtime.Networking;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Player;

namespace Swg.Client.Core.Networking.Game.Handlers.Player
{
    // Core3/MMOCoreORB/src/server/zone/packets/player/PlanetTravelPointListResponse.h:35 (PlanetTravelPointListResponse).
    [Preserve]
    internal sealed class PlanetTravelPointListResponsePacketHandler : GamePacketHandler<PlanetTravelPointListResponsePacket>
    {
        readonly ClientTravel travel;
        public override uint Opcode => PlanetTravelPointListResponsePacket.Opcode;

        [Preserve]
        public PlanetTravelPointListResponsePacketHandler(ClientTravel travel) : base(PlanetTravelPointListResponsePacket.ReadFrom)
        {
            this.travel = travel;
        }

        protected override void Handle(PlanetTravelPointListResponsePacket packet)
        {
            travel.Receive(packet);
        }
    }
}
