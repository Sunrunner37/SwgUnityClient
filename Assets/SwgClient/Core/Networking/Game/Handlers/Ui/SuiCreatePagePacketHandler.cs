using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Ui;

namespace Swg.Client.Core.Networking.Game.Handlers.Ui
{
    // Core3/MMOCoreORB/src/server/zone/packets/ui/SuiCreatePageMessage.h:17 (SuiCreatePageMessage).
    [Preserve]
    internal sealed class SuiCreatePagePacketHandler : GamePacketHandler<SuiCreatePagePacket>
    {
        public override uint Opcode => SuiCreatePagePacket.Opcode;

        [Preserve]
        public SuiCreatePagePacketHandler() : base(SuiCreatePagePacket.ReadFrom)
        {
        }

        protected override void Handle(SuiCreatePagePacket packet)
        {
            throw new NotImplementedException($"SuiCreatePageMessage (opcode 0x{Opcode:X8})");
        }
    }
}
