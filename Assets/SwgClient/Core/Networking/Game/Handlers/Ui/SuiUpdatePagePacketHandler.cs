using Swg.Client.Runtime.Networking;
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

        readonly ClientVehicleRepairs repairs;

        [Preserve]
        public SuiUpdatePagePacketHandler(ClientVehicleRepairs repairs) : base(SuiUpdatePagePacket.ReadFrom)
        {
            this.repairs = repairs;
        }

        protected override void Handle(SuiUpdatePagePacket packet)
        {
            repairs.Receive(packet.Page);
        }
    }
}
