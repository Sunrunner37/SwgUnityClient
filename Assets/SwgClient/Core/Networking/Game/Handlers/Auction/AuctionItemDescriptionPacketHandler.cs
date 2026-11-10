using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Auction;

namespace Swg.Client.Core.Networking.Game.Handlers.Auction
{
    // Core3/MMOCoreORB/src/server/zone/packets/auction/AuctionItemDescriptionMessage.h:10 (AuctionItemDescriptionMessage).
    // Core3/MMOCoreORB/src/server/zone/packets/auction/GetAuctionDetailsResponse.h:6 (GetAuctionDetailsResponse).
    // Core3/MMOCoreORB/src/server/zone/packets/scene/AttributeListMessage.h:17 (AttributeListMessage).
    [Preserve]
    internal sealed class AuctionItemDescriptionPacketHandler : GamePacketHandler<AuctionItemDescriptionPacket>
    {
        public override uint Opcode => AuctionItemDescriptionPacket.Opcode;

        [Preserve]
        public AuctionItemDescriptionPacketHandler() : base(AuctionItemDescriptionPacket.ReadFrom)
        {
        }

        protected override void Handle(AuctionItemDescriptionPacket packet)
        {
            throw new NotImplementedException($"AuctionItemDescriptionMessage (opcode 0x{Opcode:X8})");
        }
    }
}
