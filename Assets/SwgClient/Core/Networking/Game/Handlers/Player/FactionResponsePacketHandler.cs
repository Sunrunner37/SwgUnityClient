using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Player;

namespace Swg.Client.Core.Networking.Game.Handlers.Player
{
    // Core3/MMOCoreORB/src/server/zone/packets/player/FactionResponseMessage.h:10 (FactionResponseMessage).
    [Preserve]
    internal sealed class FactionResponsePacketHandler : GamePacketHandler<FactionResponsePacket>
    {
        public override uint Opcode => FactionResponsePacket.Opcode;

        [Preserve]
        public FactionResponsePacketHandler() : base(FactionResponsePacket.ReadFrom)
        {
        }

        protected override void Handle(FactionResponsePacket packet)
        {
            throw new NotImplementedException($"FactionResponseMessage (opcode 0x{Opcode:X8})");
        }
    }
}
