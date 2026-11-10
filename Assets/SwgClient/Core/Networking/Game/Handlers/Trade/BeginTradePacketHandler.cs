using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Trade;

namespace Swg.Client.Core.Networking.Game.Handlers.Trade
{
    // Core3/MMOCoreORB/src/server/zone/packets/trade/BeginTradeMessage.h:8 (BeginTradeMessage).
    [Preserve]
    internal sealed class BeginTradePacketHandler : GamePacketHandler<BeginTradePacket>
    {
        public override uint Opcode => BeginTradePacket.Opcode;

        [Preserve]
        public BeginTradePacketHandler() : base(BeginTradePacket.ReadFrom)
        {
        }

        protected override void Handle(BeginTradePacket packet)
        {
            throw new NotImplementedException($"BeginTradeMessage (opcode 0x{Opcode:X8})");
        }
    }
}
