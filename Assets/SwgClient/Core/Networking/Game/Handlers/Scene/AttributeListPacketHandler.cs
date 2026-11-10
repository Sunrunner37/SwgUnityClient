using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Scene;

namespace Swg.Client.Core.Networking.Game.Handlers.Scene
{
    // Core3/MMOCoreORB/src/server/zone/packets/scene/AttributeListMessage.h:17 (AttributeListMessage).
    [Preserve]
    internal sealed class AttributeListPacketHandler : GamePacketHandler<AttributeListPacket>
    {
        public override uint Opcode => AttributeListPacket.Opcode;

        [Preserve]
        public AttributeListPacketHandler() : base(AttributeListPacket.ReadFrom)
        {
        }

        protected override void Handle(AttributeListPacket packet)
        {
            throw new NotImplementedException($"AttributeListMessage (opcode 0x{Opcode:X8})");
        }
    }
}
