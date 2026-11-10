using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Auction;

namespace Swg.Client.Core.Networking.Game.Handlers.Auction
{
    // Core3/MMOCoreORB/src/server/zone/packets/auction/AuctionQueryHeadersResponseMessage.h:14 (AuctionQueryHeadersResponseMessage).
    [Preserve]
    internal sealed class AuctionQueryHeadersResponsePacketHandler : GamePacketHandler<AuctionQueryHeadersResponsePacket>
    {
        public override uint Opcode => AuctionQueryHeadersResponsePacket.Opcode;

        [Preserve]
        public AuctionQueryHeadersResponsePacketHandler() : base(AuctionQueryHeadersResponsePacket.ReadFrom)
        {
        }

        protected override void Handle(AuctionQueryHeadersResponsePacket packet)
        {
            throw new NotImplementedException($"AuctionQueryHeadersResponseMessage (opcode 0x{Opcode:X8})");
        }
    }
}
