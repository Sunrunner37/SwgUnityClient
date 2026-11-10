using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Auction;

namespace Swg.Client.Core.Networking.Game.Handlers.Auction
{
    // Core3/MMOCoreORB/src/server/zone/packets/auction/ItemSoldMessage.h:29 (ItemSoldMessage).
    [Preserve]
    internal sealed class ItemSoldPacketHandler : GamePacketHandler<ItemSoldPacket>
    {
        public override uint Opcode => ItemSoldPacket.Opcode;

        [Preserve]
        public ItemSoldPacketHandler() : base(ItemSoldPacket.ReadFrom)
        {
        }

        protected override void Handle(ItemSoldPacket packet)
        {
            throw new NotImplementedException($"ItemSoldMessage (opcode 0x{Opcode:X8})");
        }
    }
}
