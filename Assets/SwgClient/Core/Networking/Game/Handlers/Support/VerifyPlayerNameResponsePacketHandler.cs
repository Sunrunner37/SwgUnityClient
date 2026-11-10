using Swg.Client.Core.Networking.Game.Packets.Support;
using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Ui;

namespace Swg.Client.Core.Networking.Game.Handlers.Support
{
    // Core3/MMOCoreORB/src/server/zone/packets/ui/VerifyPlayerNameResponseMessage.h:11 (VerifyPlayerNameResponseMessage).
    [Preserve]
    internal sealed class VerifyPlayerNameResponsePacketHandler : GamePacketHandler<VerifyPlayerNameResponsePacket>
    {
        public override uint Opcode => VerifyPlayerNameResponsePacket.Opcode;

        [Preserve]
        public VerifyPlayerNameResponsePacketHandler() : base(VerifyPlayerNameResponsePacket.ReadFrom)
        {
        }

        protected override void Handle(VerifyPlayerNameResponsePacket packet)
        {
            throw new NotImplementedException($"VerifyPlayerNameResponseMessage (opcode 0x{Opcode:X8})");
        }
    }
}
