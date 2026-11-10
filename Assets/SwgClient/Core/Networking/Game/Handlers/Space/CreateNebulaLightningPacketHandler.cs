using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Space;

namespace Swg.Client.Core.Networking.Game.Handlers.Space
{
    // Core3/MMOCoreORB/src/server/zone/packets/jtl/CreateNebulaLightningMessage.h:13 (CreateNebulaLightningMessage).
    [Preserve]
    internal sealed class CreateNebulaLightningPacketHandler : GamePacketHandler<CreateNebulaLightningPacket>
    {
        public override uint Opcode => CreateNebulaLightningPacket.Opcode;

        [Preserve]
        public CreateNebulaLightningPacketHandler() : base(CreateNebulaLightningPacket.ReadFrom)
        {
        }

        protected override void Handle(CreateNebulaLightningPacket packet)
        {
            throw new NotImplementedException($"CreateNebulaLightningMessage (opcode 0x{Opcode:X8})");
        }
    }
}
