using Swg.Client.Core.Networking.Game.Packets.Support;
using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Ui;

namespace Swg.Client.Core.Networking.Game.Handlers.Support
{
    // Core3/MMOCoreORB/src/server/zone/packets/ui/CancelTicketResponseMessage.h:10 (CancelTicketResponseMessage).
    [Preserve]
    internal sealed class CancelTicketResponsePacketHandler : GamePacketHandler<CancelTicketResponsePacket>
    {
        public override uint Opcode => CancelTicketResponsePacket.Opcode;

        [Preserve]
        public CancelTicketResponsePacketHandler() : base(CancelTicketResponsePacket.ReadFrom)
        {
        }

        protected override void Handle(CancelTicketResponsePacket packet)
        {
            throw new NotImplementedException($"CancelTicketResponseMessage (opcode 0x{Opcode:X8})");
        }
    }
}
