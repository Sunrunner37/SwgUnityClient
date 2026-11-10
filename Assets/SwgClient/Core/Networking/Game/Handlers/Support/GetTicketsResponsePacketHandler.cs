using Swg.Client.Core.Networking.Game.Packets.Support;
using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Ui;

namespace Swg.Client.Core.Networking.Game.Handlers.Support
{
    // Core3/MMOCoreORB/src/server/zone/packets/ui/GetTicketsResponseMessage.h:7 (GetTicketsResponseMessage).
    [Preserve]
    internal sealed class GetTicketsResponsePacketHandler : GamePacketHandler<GetTicketsResponsePacket>
    {
        public override uint Opcode => GetTicketsResponsePacket.Opcode;

        [Preserve]
        public GetTicketsResponsePacketHandler() : base(GetTicketsResponsePacket.ReadFrom)
        {
        }

        protected override void Handle(GetTicketsResponsePacket packet)
        {
            throw new NotImplementedException($"GetTicketsResponseMessage (opcode 0x{Opcode:X8})");
        }
    }
}
