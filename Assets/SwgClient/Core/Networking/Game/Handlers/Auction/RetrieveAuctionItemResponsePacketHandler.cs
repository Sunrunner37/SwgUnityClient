using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Auction;

namespace Swg.Client.Core.Networking.Game.Handlers.Auction
{
    // Core3/MMOCoreORB/src/server/zone/packets/auction/RetrieveAuctionItemResponseMessage.h:15 (RetrieveAuctionItemResponseMessage).
    [Preserve]
    internal sealed class RetrieveAuctionItemResponsePacketHandler : GamePacketHandler<RetrieveAuctionItemResponsePacket>
    {
        public override uint Opcode => RetrieveAuctionItemResponsePacket.Opcode;

        [Preserve]
        public RetrieveAuctionItemResponsePacketHandler() : base(RetrieveAuctionItemResponsePacket.ReadFrom)
        {
        }

        protected override void Handle(RetrieveAuctionItemResponsePacket packet)
        {
            throw new NotImplementedException($"RetrieveAuctionItemResponseMessage (opcode 0x{Opcode:X8})");
        }
    }
}
