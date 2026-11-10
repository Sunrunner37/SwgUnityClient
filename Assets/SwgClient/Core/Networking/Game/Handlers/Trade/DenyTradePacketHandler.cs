using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Trade;

namespace Swg.Client.Core.Networking.Game.Handlers.Trade
{
    // Core3/MMOCoreORB/src/server/zone/packets/trade/DenyTradeMessage.h:11 (DenyTradeMessage).
    [Preserve]
    internal sealed class DenyTradePacketHandler : GamePacketHandler<DenyTradePacket>
    {
        public override uint Opcode => DenyTradePacket.Opcode;

        [Preserve]
        public DenyTradePacketHandler() : base(DenyTradePacket.ReadFrom)
        {
        }

        protected override void Handle(DenyTradePacket packet)
        {
            throw new NotImplementedException($"DenyTradeMessage (opcode 0x{Opcode:X8})");
        }
    }
}
