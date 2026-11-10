using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Player;

namespace Swg.Client.Core.Networking.Game.Handlers.Player
{
    // Core3/MMOCoreORB/src/server/zone/packets/player/GuildResponseMessage.h:12 (GuildResponseMessage).
    [Preserve]
    internal sealed class GuildResponsePacketHandler : GamePacketHandler<GuildResponsePacket>
    {
        public override uint Opcode => GuildResponsePacket.Opcode;

        [Preserve]
        public GuildResponsePacketHandler() : base(GuildResponsePacket.ReadFrom)
        {
        }

        protected override void Handle(GuildResponsePacket packet)
        {
            throw new NotImplementedException($"GuildResponseMessage (opcode 0x{Opcode:X8})");
        }
    }
}
