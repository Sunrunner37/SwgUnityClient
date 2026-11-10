using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Trade;

namespace Swg.Client.Core.Networking.Game.Handlers.Trade
{
    // Core3/MMOCoreORB/src/server/zone/packets/trade/AbortTradeMessage.h:8 (AbortTradeMessage).
    [Preserve]
    internal sealed class AbortTradePacketHandler : GamePacketHandler<AbortTradePacket>
    {
        public override uint Opcode => AbortTradePacket.Opcode;

        [Preserve]
        public AbortTradePacketHandler() : base(AbortTradePacket.ReadFrom)
        {
        }

        protected override void Handle(AbortTradePacket packet)
        {
            throw new NotImplementedException($"AbortTradeMessage (opcode 0x{Opcode:X8})");
        }
    }
}
