using Swg.Client.Runtime.Networking;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Player;

namespace Swg.Client.Core.Networking.Game.Handlers.Player
{
    // Core3/MMOCoreORB/src/server/zone/packets/player/EnterTicketPurchaseModeMessage.h:11 (EnterTicketPurchaseModeMessage).
    [Preserve]
    internal sealed class EnterTicketPurchaseModePacketHandler : GamePacketHandler<EnterTicketPurchaseModePacket>
    {
        readonly ClientTravel travel;
        public override uint Opcode => EnterTicketPurchaseModePacket.Opcode;

        [Preserve]
        public EnterTicketPurchaseModePacketHandler(ClientTravel travel) : base(EnterTicketPurchaseModePacket.ReadFrom)
        {
            this.travel = travel;
        }

        protected override void Handle(EnterTicketPurchaseModePacket packet)
        {
            travel.Receive(packet);
        }
    }
}
