using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Auction;

namespace Swg.Client.Core.Networking.Game.Handlers.Auction
{
    // Core3/MMOCoreORB/src/server/zone/packets/auction/BidAuctionResponseMessage.h:19 (BidAuctionResponseMessage).
    [Preserve]
    internal sealed class BidAuctionResponsePacketHandler : GamePacketHandler<BidAuctionResponsePacket>
    {
        public override uint Opcode => BidAuctionResponsePacket.Opcode;

        [Preserve]
        public BidAuctionResponsePacketHandler() : base(BidAuctionResponsePacket.ReadFrom)
        {
        }

        protected override void Handle(BidAuctionResponsePacket packet)
        {
            throw new NotImplementedException($"BidAuctionResponseMessage (opcode 0x{Opcode:X8})");
        }
    }
}
