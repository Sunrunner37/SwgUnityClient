using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Ui;

namespace Swg.Client.Core.Networking.Game.Handlers.Ui
{
    // Core3/MMOCoreORB/src/server/zone/packets/ui/CreateClientPathMessage.h:13 (CreateClientPathMessage).
    [Preserve]
    internal sealed class CreateClientPathPacketHandler : GamePacketHandler<CreateClientPathPacket>
    {
        public override uint Opcode => CreateClientPathPacket.Opcode;

        [Preserve]
        public CreateClientPathPacketHandler() : base(CreateClientPathPacket.ReadFrom)
        {
        }

        protected override void Handle(CreateClientPathPacket packet)
        {
            throw new NotImplementedException($"CreateClientPathMessage (opcode 0x{Opcode:X8})");
        }
    }
}
