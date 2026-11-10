using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Trade;

namespace Swg.Client.Core.Networking.Game.Handlers.Trade
{
    // Core3/MMOCoreORB/src/server/zone/packets/trade/AcceptTransactionMessage.h:8 (AcceptTransactionMessage).
    [Preserve]
    internal sealed class AcceptTransactionPacketHandler : GamePacketHandler<AcceptTransactionPacket>
    {
        public override uint Opcode => AcceptTransactionPacket.Opcode;

        [Preserve]
        public AcceptTransactionPacketHandler() : base(AcceptTransactionPacket.ReadFrom)
        {
        }

        protected override void Handle(AcceptTransactionPacket packet)
        {
            throw new NotImplementedException($"AcceptTransactionMessage (opcode 0x{Opcode:X8})");
        }
    }
}
