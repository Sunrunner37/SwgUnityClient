using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Space;

namespace Swg.Client.Core.Networking.Game.Handlers.Space
{
    // Core3/MMOCoreORB/src/server/zone/packets/jtl/CreateProjectileMessage.h:13 (CreateProjectileMessage).
    [Preserve]
    internal sealed class CreateProjectilePacketHandler : GamePacketHandler<CreateProjectilePacket>
    {
        public override uint Opcode => CreateProjectilePacket.Opcode;

        [Preserve]
        public CreateProjectilePacketHandler() : base(CreateProjectilePacket.ReadFrom)
        {
        }

        protected override void Handle(CreateProjectilePacket packet)
        {
            throw new NotImplementedException($"CreateProjectileMessage (opcode 0x{Opcode:X8})");
        }
    }
}
