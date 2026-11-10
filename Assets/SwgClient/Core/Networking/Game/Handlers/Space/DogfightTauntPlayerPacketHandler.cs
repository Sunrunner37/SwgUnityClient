using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Space;

namespace Swg.Client.Core.Networking.Game.Handlers.Space
{
    // Core3/MMOCoreORB/src/server/zone/packets/jtl/DogfightTauntPlayerMessage.h:13 (DogfightTauntPlayerMessage).
    [Preserve]
    internal sealed class DogfightTauntPlayerPacketHandler : GamePacketHandler<DogfightTauntPlayerPacket>
    {
        public override uint Opcode => DogfightTauntPlayerPacket.Opcode;

        [Preserve]
        public DogfightTauntPlayerPacketHandler() : base(DogfightTauntPlayerPacket.ReadFrom)
        {
        }

        protected override void Handle(DogfightTauntPlayerPacket packet)
        {
            throw new NotImplementedException($"DogfightTauntPlayerMessage (opcode 0x{Opcode:X8})");
        }
    }
}
