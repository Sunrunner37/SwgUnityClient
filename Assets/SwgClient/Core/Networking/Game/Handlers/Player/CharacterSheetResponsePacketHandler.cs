using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Player;

namespace Swg.Client.Core.Networking.Game.Handlers.Player
{
    // Core3/MMOCoreORB/src/server/zone/packets/player/CharacterSheetResponseMessage.h:11 (CharacterSheetResponseMessage).
    [Preserve]
    internal sealed class CharacterSheetResponsePacketHandler : GamePacketHandler<CharacterSheetResponsePacket>
    {
        public override uint Opcode => CharacterSheetResponsePacket.Opcode;

        [Preserve]
        public CharacterSheetResponsePacketHandler() : base(CharacterSheetResponsePacket.ReadFrom)
        {
        }

        protected override void Handle(CharacterSheetResponsePacket packet)
        {
            throw new NotImplementedException($"CharacterSheetResponseMessage (opcode 0x{Opcode:X8})");
        }
    }
}
