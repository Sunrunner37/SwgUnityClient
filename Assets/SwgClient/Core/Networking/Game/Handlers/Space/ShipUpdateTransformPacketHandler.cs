using Swg.Client.Core.Networking.Game.Packets.Space;
using System;
using UnityEngine.Scripting;

namespace Swg.Client.Core.Networking.Game.Handlers.Space
{
    // Core3/MMOCoreORB/src/server/zone/packets/ship/ShipUpdateTransformMessage.h:16 (ShipUpdateTransformMessage).
    [Preserve]
    internal sealed class ShipUpdateTransformPacketHandler : GamePacketHandler<ShipUpdateTransformPacket>
    {
        public override uint Opcode => ShipUpdateTransformPacket.Opcode;

        [Preserve]
        public ShipUpdateTransformPacketHandler() : base(ShipUpdateTransformPacket.ReadFrom)
        {
        }

        protected override void Handle(ShipUpdateTransformPacket packet)
        {
            throw new NotImplementedException($"ShipUpdateTransformMessage (opcode 0x{Opcode:X8})");
        }
    }
}
