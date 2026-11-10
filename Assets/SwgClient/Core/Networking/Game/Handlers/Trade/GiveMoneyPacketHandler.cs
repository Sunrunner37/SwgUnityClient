using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Trade;

namespace Swg.Client.Core.Networking.Game.Handlers.Trade
{
    // Core3/MMOCoreORB/src/server/zone/packets/trade/GiveMoneyMessage.h:8 (GiveMoneyMessage).
    [Preserve]
    internal sealed class GiveMoneyPacketHandler : GamePacketHandler<GiveMoneyPacket>
    {
        public override uint Opcode => GiveMoneyPacket.Opcode;

        [Preserve]
        public GiveMoneyPacketHandler() : base(GiveMoneyPacket.ReadFrom)
        {
        }

        protected override void Handle(GiveMoneyPacket packet)
        {
            throw new NotImplementedException($"GiveMoneyMessage (opcode 0x{Opcode:X8})");
        }
    }
}
