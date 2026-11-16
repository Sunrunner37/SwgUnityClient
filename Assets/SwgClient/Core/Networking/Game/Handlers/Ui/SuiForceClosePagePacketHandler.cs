using Swg.Client.Runtime.Networking;
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

        readonly ClientVehicleRepairs repairs;

        [Preserve]
        public SuiForceClosePagePacketHandler(ClientVehicleRepairs repairs) : base(SuiForceClosePagePacket.ReadFrom)
        {
            this.repairs = repairs;
        }

        protected override void Handle(SuiForceClosePagePacket packet)
        {
            repairs.Close(packet.PageId);
        }
    }
}
