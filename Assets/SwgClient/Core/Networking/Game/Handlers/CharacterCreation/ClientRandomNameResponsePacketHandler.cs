using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.CharacterCreation;

namespace Swg.Client.Core.Networking.Game.Handlers.CharacterCreation
{
    // Core3/MMOCoreORB/src/server/zone/packets/charcreation/ClientRandomNameResponse.h:10 (ClientRandomNameResponse).
    [Preserve]
    internal sealed class ClientRandomNameResponsePacketHandler : GamePacketHandler<ClientRandomNameResponsePacket>
    {
        public override uint Opcode => ClientRandomNameResponsePacket.Opcode;

        [Preserve]
        public ClientRandomNameResponsePacketHandler() : base(ClientRandomNameResponsePacket.ReadFrom)
        {
        }

        protected override void Handle(ClientRandomNameResponsePacket packet)
        {
            throw new NotImplementedException($"ClientRandomNameResponse (opcode 0x{Opcode:X8})");
        }
    }
}
