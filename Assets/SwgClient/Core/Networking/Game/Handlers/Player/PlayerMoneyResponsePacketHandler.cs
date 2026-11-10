using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Player;

namespace Swg.Client.Core.Networking.Game.Handlers.Player
{
    // Core3/MMOCoreORB/src/server/zone/packets/player/PlayerMoneyResponseMessage.h:11 (PlayerMoneyResponseMessage).
    [Preserve]
    internal sealed class PlayerMoneyResponsePacketHandler : GamePacketHandler<PlayerMoneyResponsePacket>
    {
        public override uint Opcode => PlayerMoneyResponsePacket.Opcode;

        [Preserve]
        public PlayerMoneyResponsePacketHandler() : base(PlayerMoneyResponsePacket.ReadFrom)
        {
        }

        protected override void Handle(PlayerMoneyResponsePacket packet)
        {
            throw new NotImplementedException($"PlayerMoneyResponseMessage (opcode 0x{Opcode:X8})");
        }
    }
}
