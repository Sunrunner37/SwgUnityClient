using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Player;

namespace Swg.Client.Core.Networking.Game.Handlers.Player
{
    // Core3/MMOCoreORB/src/server/zone/packets/player/EnterTicketPurchaseModeMessage.h:11 (EnterTicketPurchaseModeMessage).
    [Preserve]
    internal sealed class EnterTicketPurchaseModePacketHandler : GamePacketHandler<EnterTicketPurchaseModePacket>
    {
        public override uint Opcode => EnterTicketPurchaseModePacket.Opcode;

        [Preserve]
        public EnterTicketPurchaseModePacketHandler() : base(EnterTicketPurchaseModePacket.ReadFrom)
        {
        }

        protected override void Handle(EnterTicketPurchaseModePacket packet)
        {
            throw new NotImplementedException($"EnterTicketPurchaseModeMessage (opcode 0x{Opcode:X8})");
        }
    }
}
