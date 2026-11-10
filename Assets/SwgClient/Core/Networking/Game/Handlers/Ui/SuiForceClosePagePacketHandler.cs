using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Ui;

namespace Swg.Client.Core.Networking.Game.Handlers.Ui
{
    // Core3/MMOCoreORB/src/server/zone/packets/ui/SuiForceClosePage.h:8 (SuiForceClosePage).
    [Preserve]
    internal sealed class SuiForceClosePagePacketHandler : GamePacketHandler<SuiForceClosePagePacket>
    {
        public override uint Opcode => SuiForceClosePagePacket.Opcode;

        [Preserve]
        public SuiForceClosePagePacketHandler() : base(SuiForceClosePagePacket.ReadFrom)
        {
        }

        protected override void Handle(SuiForceClosePagePacket packet)
        {
            throw new NotImplementedException($"SuiForceClosePage (opcode 0x{Opcode:X8})");
        }
    }
}
