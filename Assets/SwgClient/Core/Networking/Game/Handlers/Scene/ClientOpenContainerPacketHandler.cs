using Swg.Client.Runtime.Networking;
using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Scene;

namespace Swg.Client.Core.Networking.Game.Handlers.Scene
{
    // Core3/MMOCoreORB/src/server/zone/packets/scene/ClientOpenContainerMessage.h:12 (ClientOpenContainerMessage).
    [Preserve]
    internal sealed class ClientOpenContainerPacketHandler : GamePacketHandler<ClientOpenContainerPacket>
    {
        readonly ClientInventory inventory;
        public override uint Opcode => ClientOpenContainerPacket.Opcode;

        [Preserve]
        public ClientOpenContainerPacketHandler(ClientInventory inventory) : base(ClientOpenContainerPacket.ReadFrom)
        {
            this.inventory = inventory;
        }

        protected override void Handle(ClientOpenContainerPacket packet)
        {
            inventory.Receive(packet);
        }
    }
}
