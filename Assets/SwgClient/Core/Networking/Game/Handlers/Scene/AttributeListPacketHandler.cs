using Swg.Client.Runtime.Networking;
using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Scene;

namespace Swg.Client.Core.Networking.Game.Handlers.Scene
{
    // Core3/MMOCoreORB/src/server/zone/packets/scene/AttributeListMessage.h:17 (AttributeListMessage).
    [Preserve]
    internal sealed class AttributeListPacketHandler : GamePacketHandler<AttributeListPacket>
    {
        readonly ClientObjectInteractions interactions;
        public override uint Opcode => AttributeListPacket.Opcode;

        [Preserve]
        public AttributeListPacketHandler(ClientObjectInteractions interactions) : base(AttributeListPacket.ReadFrom)
        {
            this.interactions = interactions;
        }

        protected override void Handle(AttributeListPacket packet)
        {
            interactions.Receive(packet);
        }
    }
}
