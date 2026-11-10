using Swg.Client.Core.Networking.Game.Packets.Movement;
using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Scene;

namespace Swg.Client.Core.Networking.Game.Handlers.Movement
{
    // Core3/MMOCoreORB/src/server/zone/packets/scene/DebugTransformMessage.h:13 (DebugTransformMessage).
    [Preserve]
    internal sealed class DebugTransformPacketHandler : GamePacketHandler<DebugTransformPacket>
    {
        public override uint Opcode => DebugTransformPacket.Opcode;

        [Preserve]
        public DebugTransformPacketHandler() : base(DebugTransformPacket.ReadFrom)
        {
        }

        protected override void Handle(DebugTransformPacket packet)
        {
            throw new NotImplementedException($"DebugTransformMessage (opcode 0x{Opcode:X8})");
        }
    }
}
