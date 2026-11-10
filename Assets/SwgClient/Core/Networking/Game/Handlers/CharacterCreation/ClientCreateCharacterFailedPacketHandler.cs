using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.CharacterCreation;

namespace Swg.Client.Core.Networking.Game.Handlers.CharacterCreation
{
    // Core3/MMOCoreORB/src/server/zone/packets/charcreation/ClientCreateCharacterFailed.h:13 (ClientCreateCharacterFailed).
    [Preserve]
    internal sealed class ClientCreateCharacterFailedPacketHandler : GamePacketHandler<ClientCreateCharacterFailedPacket>
    {
        public override uint Opcode => ClientCreateCharacterFailedPacket.Opcode;

        [Preserve]
        public ClientCreateCharacterFailedPacketHandler() : base(ClientCreateCharacterFailedPacket.ReadFrom)
        {
        }

        protected override void Handle(ClientCreateCharacterFailedPacket packet)
        {
            throw new NotImplementedException($"ClientCreateCharacterFailed (opcode 0x{Opcode:X8})");
        }
    }
}
