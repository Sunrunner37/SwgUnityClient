using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Ui;

namespace Swg.Client.Core.Networking.Game.Handlers.Ui
{
    // Core3/MMOCoreORB/src/server/zone/packets/ui/SuiUpdatePageMessage.h:7 (SuiUpdatePageMessage).
    [Preserve]
    internal sealed class SuiUpdatePagePacketHandler : GamePacketHandler<SuiUpdatePagePacket>
    {
        public override uint Opcode => SuiUpdatePagePacket.Opcode;

        [Preserve]
        public SuiUpdatePagePacketHandler() : base(SuiUpdatePagePacket.ReadFrom)
        {
        }

        protected override void Handle(SuiUpdatePagePacket packet)
        {
            throw new NotImplementedException($"SuiUpdatePageMessage (opcode 0x{Opcode:X8})");
        }
    }
}
