using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Trade;

namespace Swg.Client.Core.Networking.Game.Handlers.Trade
{
    // Core3/MMOCoreORB/src/server/zone/packets/trade/UnAcceptTransactionMessage.h:8 (UnAcceptTransactionMessage).
    [Preserve]
    internal sealed class UnAcceptTransactionPacketHandler : GamePacketHandler<UnAcceptTransactionPacket>
    {
        public override uint Opcode => UnAcceptTransactionPacket.Opcode;

        [Preserve]
        public UnAcceptTransactionPacketHandler() : base(UnAcceptTransactionPacket.ReadFrom)
        {
        }

        protected override void Handle(UnAcceptTransactionPacket packet)
        {
            throw new NotImplementedException($"UnAcceptTransactionMessage (opcode 0x{Opcode:X8})");
        }
    }
}
