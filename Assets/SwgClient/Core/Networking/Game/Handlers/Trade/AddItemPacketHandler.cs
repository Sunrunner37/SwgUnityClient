using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Trade;

namespace Swg.Client.Core.Networking.Game.Handlers.Trade
{
    // Core3/MMOCoreORB/src/server/zone/packets/trade/AddItemMessage.h:8 (AddItemMessage).
    [Preserve]
    internal sealed class AddItemPacketHandler : GamePacketHandler<AddItemPacket>
    {
        public override uint Opcode => AddItemPacket.Opcode;

        [Preserve]
        public AddItemPacketHandler() : base(AddItemPacket.ReadFrom)
        {
        }

        protected override void Handle(AddItemPacket packet)
        {
            throw new NotImplementedException($"AddItemMessage (opcode 0x{Opcode:X8})");
        }
    }
}
