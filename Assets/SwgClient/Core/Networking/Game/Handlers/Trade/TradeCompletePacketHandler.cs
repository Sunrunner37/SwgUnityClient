using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Trade;

namespace Swg.Client.Core.Networking.Game.Handlers.Trade
{
    // Core3/MMOCoreORB/src/server/zone/packets/trade/TradeCompleteMessage.h:8 (TradeCompleteMessage).
    [Preserve]
    internal sealed class TradeCompletePacketHandler : GamePacketHandler<TradeCompletePacket>
    {
        public override uint Opcode => TradeCompletePacket.Opcode;

        [Preserve]
        public TradeCompletePacketHandler() : base(TradeCompletePacket.ReadFrom)
        {
        }

        protected override void Handle(TradeCompletePacket packet)
        {
            throw new NotImplementedException($"TradeCompleteMessage (opcode 0x{Opcode:X8})");
        }
    }
}
