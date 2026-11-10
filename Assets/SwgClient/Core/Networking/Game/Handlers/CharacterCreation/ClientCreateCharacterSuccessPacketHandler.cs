using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.CharacterCreation;

namespace Swg.Client.Core.Networking.Game.Handlers.CharacterCreation
{
    // Core3/MMOCoreORB/src/server/zone/packets/charcreation/ClientCreateCharacterSuccess.h:13 (ClientCreateCharacterSuccess).
    [Preserve]
    internal sealed class ClientCreateCharacterSuccessPacketHandler : GamePacketHandler<ClientCreateCharacterSuccessPacket>
    {
        public override uint Opcode => ClientCreateCharacterSuccessPacket.Opcode;

        [Preserve]
        public ClientCreateCharacterSuccessPacketHandler() : base(ClientCreateCharacterSuccessPacket.ReadFrom)
        {
        }

        protected override void Handle(ClientCreateCharacterSuccessPacket packet)
        {
            throw new NotImplementedException($"ClientCreateCharacterSuccess (opcode 0x{Opcode:X8})");
        }
    }
}
