using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Ui;

namespace Swg.Client.Core.Networking.Game.Handlers.Ui
{
    // Core3/MMOCoreORB/src/server/zone/packets/ui/ConGenericMessage.h:13 (ConGenericMessage).
    [Preserve]
    internal sealed class ConGenericPacketHandler : GamePacketHandler<ConGenericPacket>
    {
        public override uint Opcode => ConGenericPacket.Opcode;

        [Preserve]
        public ConGenericPacketHandler() : base(ConGenericPacket.ReadFrom)
        {
        }

        protected override void Handle(ConGenericPacket packet)
        {
            throw new NotImplementedException($"ConGenericMessage (opcode 0x{Opcode:X8})");
        }
    }
}
