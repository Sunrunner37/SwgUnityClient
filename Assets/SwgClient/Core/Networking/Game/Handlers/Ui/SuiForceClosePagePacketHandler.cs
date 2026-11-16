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
        readonly ClientTravel travel;

        [Preserve]
        public SuiForceClosePagePacketHandler(ClientVehicleRepairs repairs, ClientTravel travel) : base(SuiForceClosePagePacket.ReadFrom)
        {
            this.repairs = repairs;
            this.travel = travel;
        }

        protected override void Handle(SuiForceClosePagePacket packet)
        {
            travel.CloseTickets(packet.PageId);
            repairs.Close(packet.PageId);
        }
    }
}
