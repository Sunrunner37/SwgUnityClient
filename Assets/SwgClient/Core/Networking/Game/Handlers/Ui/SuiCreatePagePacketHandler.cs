using Swg.Client.Runtime.Networking;
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

        readonly ClientVehicleRepairs repairs;
        readonly ClientTravel travel;

        [Preserve]
        public SuiCreatePagePacketHandler(ClientVehicleRepairs repairs, ClientTravel travel) : base(SuiCreatePagePacket.ReadFrom)
        {
            this.repairs = repairs;
            this.travel = travel;
        }

        protected override void Handle(SuiCreatePagePacket packet)
        {
            if (!travel.TryReceive(packet.Page))
                repairs.Receive(packet.Page);
        }
    }
}
