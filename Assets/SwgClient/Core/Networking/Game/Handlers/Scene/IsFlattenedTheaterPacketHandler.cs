using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Scene;

namespace Swg.Client.Core.Networking.Game.Handlers.Scene
{
    // Core3/MMOCoreORB/src/server/zone/packets/scene/IsFlattenedTheaterMessage.h:13 (IsFlattenedTheaterMessage).
    [Preserve]
    internal sealed class IsFlattenedTheaterPacketHandler : GamePacketHandler<IsFlattenedTheaterPacket>
    {
        public override uint Opcode => IsFlattenedTheaterPacket.Opcode;

        [Preserve]
        public IsFlattenedTheaterPacketHandler() : base(IsFlattenedTheaterPacket.ReadFrom)
        {
        }

        protected override void Handle(IsFlattenedTheaterPacket packet)
        {
            throw new NotImplementedException($"IsFlattenedTheaterMessage (opcode 0x{Opcode:X8})");
        }
    }
}
