using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Ui;

namespace Swg.Client.Core.Networking.Game.Handlers.Ui
{
    // Core3/MMOCoreORB/src/server/zone/packets/ui/ClientMfdStatusUpdateMessage.h:12 (ClientMfdStatusUpdateMessage).
    [Preserve]
    internal sealed class ClientMfdStatusUpdatePacketHandler : GamePacketHandler<ClientMfdStatusUpdatePacket>
    {
        public override uint Opcode => ClientMfdStatusUpdatePacket.Opcode;

        [Preserve]
        public ClientMfdStatusUpdatePacketHandler() : base(ClientMfdStatusUpdatePacket.ReadFrom)
        {
        }

        protected override void Handle(ClientMfdStatusUpdatePacket packet)
        {
            throw new NotImplementedException($"ClientMfdStatusUpdateMessage (opcode 0x{Opcode:X8})");
        }
    }
}
