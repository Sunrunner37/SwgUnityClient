using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Player;

namespace Swg.Client.Core.Networking.Game.Handlers.Player
{
    // Core3/MMOCoreORB/src/server/zone/packets/player/BadgesResponseMessage.h:12 (BadgesResponseMessage).
    [Preserve]
    internal sealed class BadgesResponsePacketHandler : GamePacketHandler<BadgesResponsePacket>
    {
        public override uint Opcode => BadgesResponsePacket.Opcode;

        [Preserve]
        public BadgesResponsePacketHandler() : base(BadgesResponsePacket.ReadFrom)
        {
        }

        protected override void Handle(BadgesResponsePacket packet)
        {
            throw new NotImplementedException($"BadgesResponseMessage (opcode 0x{Opcode:X8})");
        }
    }
}
