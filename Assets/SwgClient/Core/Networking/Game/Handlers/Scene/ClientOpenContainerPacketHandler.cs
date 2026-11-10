using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Scene;

namespace Swg.Client.Core.Networking.Game.Handlers.Scene
{
    // Core3/MMOCoreORB/src/server/zone/packets/scene/ClientOpenContainerMessage.h:12 (ClientOpenContainerMessage).
    [Preserve]
    internal sealed class ClientOpenContainerPacketHandler : GamePacketHandler<ClientOpenContainerPacket>
    {
        public override uint Opcode => ClientOpenContainerPacket.Opcode;

        [Preserve]
        public ClientOpenContainerPacketHandler() : base(ClientOpenContainerPacket.ReadFrom)
        {
        }

        protected override void Handle(ClientOpenContainerPacket packet)
        {
            throw new NotImplementedException($"ClientOpenContainerMessage (opcode 0x{Opcode:X8})");
        }
    }
}
