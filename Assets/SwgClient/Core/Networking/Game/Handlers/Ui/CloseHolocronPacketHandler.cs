using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Ui;

namespace Swg.Client.Core.Networking.Game.Handlers.Ui
{
    // Core3/MMOCoreORB/src/server/zone/packets/ui/CloseHolocronMessage.h:13 (CloseHolocronMessage).
    [Preserve]
    internal sealed class CloseHolocronPacketHandler : GamePacketHandler<CloseHolocronPacket>
    {
        public override uint Opcode => CloseHolocronPacket.Opcode;

        [Preserve]
        public CloseHolocronPacketHandler() : base(CloseHolocronPacket.ReadFrom)
        {
        }

        protected override void Handle(CloseHolocronPacket packet)
        {
            throw new NotImplementedException($"CloseHolocronMessage (opcode 0x{Opcode:X8})");
        }
    }
}
