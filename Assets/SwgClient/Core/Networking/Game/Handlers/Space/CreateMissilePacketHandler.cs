using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Space;

namespace Swg.Client.Core.Networking.Game.Handlers.Space
{
    // Core3/MMOCoreORB/src/server/zone/packets/jtl/CreateMissileMessage.h:13 (CreateMissileMessage).
    [Preserve]
    internal sealed class CreateMissilePacketHandler : GamePacketHandler<CreateMissilePacket>
    {
        public override uint Opcode => CreateMissilePacket.Opcode;

        [Preserve]
        public CreateMissilePacketHandler() : base(CreateMissilePacket.ReadFrom)
        {
        }

        protected override void Handle(CreateMissilePacket packet)
        {
            throw new NotImplementedException($"CreateMissileMessage (opcode 0x{Opcode:X8})");
        }
    }
}
