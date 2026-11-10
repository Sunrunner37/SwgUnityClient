using Swg.Client.Core.Networking.Game.Packets.Support;
using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Ui;

namespace Swg.Client.Core.Networking.Game.Handlers.Support
{
    // Core3/MMOCoreORB/src/server/zone/packets/ui/NewTicketActivityResponseMessage.h:11 (NewTicketActivityResponseMessage).
    [Preserve]
    internal sealed class NewTicketActivityResponsePacketHandler : GamePacketHandler<NewTicketActivityResponsePacket>
    {
        public override uint Opcode => NewTicketActivityResponsePacket.Opcode;

        [Preserve]
        public NewTicketActivityResponsePacketHandler() : base(NewTicketActivityResponsePacket.ReadFrom)
        {
        }

        protected override void Handle(NewTicketActivityResponsePacket packet)
        {
            throw new NotImplementedException($"NewTicketActivityResponseMessage (opcode 0x{Opcode:X8})");
        }
    }
}
