using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Auction;

namespace Swg.Client.Core.Networking.Game.Handlers.Auction
{
    // Core3/MMOCoreORB/src/server/zone/packets/auction/CancelLiveAuctionResponseMessage.h:17 (CancelLiveAuctionResponseMessage).
    [Preserve]
    internal sealed class CancelLiveAuctionResponsePacketHandler : GamePacketHandler<CancelLiveAuctionResponsePacket>
    {
        public override uint Opcode => CancelLiveAuctionResponsePacket.Opcode;

        [Preserve]
        public CancelLiveAuctionResponsePacketHandler() : base(CancelLiveAuctionResponsePacket.ReadFrom)
        {
        }

        protected override void Handle(CancelLiveAuctionResponsePacket packet)
        {
            throw new NotImplementedException($"CancelLiveAuctionResponseMessage (opcode 0x{Opcode:X8})");
        }
    }
}
