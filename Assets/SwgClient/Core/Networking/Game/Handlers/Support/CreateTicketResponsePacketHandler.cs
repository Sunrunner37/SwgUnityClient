using Swg.Client.Core.Networking.Game.Packets.Support;
using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Ui;

namespace Swg.Client.Core.Networking.Game.Handlers.Support
{
    // Core3/MMOCoreORB/src/server/zone/packets/ui/CreateTicketResponseMessage.h:13 (CreateTicketResponseMessage).
    [Preserve]
    internal sealed class CreateTicketResponsePacketHandler : GamePacketHandler<CreateTicketResponsePacket>
    {
        public override uint Opcode => CreateTicketResponsePacket.Opcode;

        [Preserve]
        public CreateTicketResponsePacketHandler() : base(CreateTicketResponsePacket.ReadFrom)
        {
        }

        protected override void Handle(CreateTicketResponsePacket packet)
        {
            throw new NotImplementedException($"CreateTicketResponseMessage (opcode 0x{Opcode:X8})");
        }
    }
}
