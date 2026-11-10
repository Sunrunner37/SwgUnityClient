using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Ui;

namespace Swg.Client.Core.Networking.Game.Handlers.Ui
{
    // Core3/MMOCoreORB/src/server/zone/packets/ui/OpenHolocronToPageMessage.h:11 (OpenHolocronToPageMessage).
    [Preserve]
    internal sealed class OpenHolocronToPagePacketHandler : GamePacketHandler<OpenHolocronToPagePacket>
    {
        public override uint Opcode => OpenHolocronToPagePacket.Opcode;

        [Preserve]
        public OpenHolocronToPagePacketHandler() : base(OpenHolocronToPagePacket.ReadFrom)
        {
        }

        protected override void Handle(OpenHolocronToPagePacket packet)
        {
            throw new NotImplementedException($"OpenHolocronToPageMessage (opcode 0x{Opcode:X8})");
        }
    }
}
