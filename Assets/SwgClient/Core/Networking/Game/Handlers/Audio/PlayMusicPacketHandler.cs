using Swg.Client.Core.Networking.Game.Packets.Audio;
using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Player;

namespace Swg.Client.Core.Networking.Game.Handlers.Audio
{
    // Core3/MMOCoreORB/src/server/zone/packets/player/PlayMusicMessage.h:10 (PlayMusicMessage).
    [Preserve]
    internal sealed class PlayMusicPacketHandler : GamePacketHandler<PlayMusicPacket>
    {
        public override uint Opcode => PlayMusicPacket.Opcode;

        [Preserve]
        public PlayMusicPacketHandler() : base(PlayMusicPacket.ReadFrom)
        {
        }

        protected override void Handle(PlayMusicPacket packet)
        {
            throw new NotImplementedException($"PlayMusicMessage (opcode 0x{Opcode:X8})");
        }
    }
}
