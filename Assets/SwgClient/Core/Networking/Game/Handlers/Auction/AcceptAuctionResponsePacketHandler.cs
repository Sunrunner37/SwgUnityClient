using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Auction;

namespace Swg.Client.Core.Networking.Game.Handlers.Auction
{
    // Core3/MMOCoreORB/src/server/zone/packets/auction/AcceptAuctionResponseMessage.h:13 (AcceptAuctionResponseMessage).
    [Preserve]
    internal sealed class AcceptAuctionResponsePacketHandler : GamePacketHandler<AcceptAuctionResponsePacket>
    {
        public override uint Opcode => AcceptAuctionResponsePacket.Opcode;

        [Preserve]
        public AcceptAuctionResponsePacketHandler() : base(AcceptAuctionResponsePacket.ReadFrom)
        {
        }

        protected override void Handle(AcceptAuctionResponsePacket packet)
        {
            throw new NotImplementedException($"AcceptAuctionResponseMessage (opcode 0x{Opcode:X8})");
        }
    }
}
