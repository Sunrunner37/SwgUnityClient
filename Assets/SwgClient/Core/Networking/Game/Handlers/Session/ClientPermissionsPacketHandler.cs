using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Login.Packets;

namespace Swg.Client.Core.Networking.Game.Handlers.Session
{
    // Core3/MMOCoreORB/src/server/zone/packets/zone/ClientPermissionsMessage.h:10 (ClientPermissionsMessage).
    [Preserve]
    internal sealed class ClientPermissionsPacketHandler : GamePacketHandler<ClientPermissionsPacket>
    {
        public override uint Opcode => ClientPermissionsPacket.Opcode;

        [Preserve]
        public ClientPermissionsPacketHandler() : base(ClientPermissionsPacket.ReadFrom)
        {
        }

        protected override void Handle(ClientPermissionsPacket packet)
        {
            throw new NotImplementedException($"ClientPermissionsMessage (opcode 0x{Opcode:X8})");
        }
    }
}
