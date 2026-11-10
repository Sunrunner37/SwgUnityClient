using Swg.Client.Core.Networking.Game.Packets.Space;
using System;
using UnityEngine.Scripting;

namespace Swg.Client.Core.Networking.Game.Handlers.Space
{
    // Core3/MMOCoreORB/src/server/zone/packets/ship/ShipUpdateTransformCollisionMessage.h:13 (ShipUpdateTransformCollisionMessage).
    [Preserve]
    internal sealed class ShipUpdateTransformCollisionPacketHandler : GamePacketHandler<ShipUpdateTransformCollisionPacket>
    {
        public override uint Opcode => ShipUpdateTransformCollisionPacket.Opcode;

        [Preserve]
        public ShipUpdateTransformCollisionPacketHandler() : base(ShipUpdateTransformCollisionPacket.ReadFrom)
        {
        }

        protected override void Handle(ShipUpdateTransformCollisionPacket packet)
        {
            throw new NotImplementedException($"ShipUpdateTransformCollisionMessage (opcode 0x{Opcode:X8})");
        }
    }
}
