using Swg.Client.Core.Networking.Game.Packets.Effects;
using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Scene;

namespace Swg.Client.Core.Networking.Game.Handlers.Effects
{
    // Core3/MMOCoreORB/src/server/zone/packets/scene/PlayClientEffectLocMessage.h:10 (PlayClientEffectLoc).
    [Preserve]
    internal sealed class PlayClientEffectLocPacketHandler : GamePacketHandler<PlayClientEffectLocPacket>
    {
        public override uint Opcode => PlayClientEffectLocPacket.Opcode;

        [Preserve]
        public PlayClientEffectLocPacketHandler() : base(PlayClientEffectLocPacket.ReadFrom)
        {
        }

        protected override void Handle(PlayClientEffectLocPacket packet)
        {
            throw new NotImplementedException($"PlayClientEffectLoc (opcode 0x{Opcode:X8})");
        }
    }
}
