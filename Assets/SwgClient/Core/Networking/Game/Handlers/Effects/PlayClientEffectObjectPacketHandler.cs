using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Effects;

namespace Swg.Client.Core.Networking.Game.Handlers.Effects
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/PlayClientEffectObjectMessage.h:12 (PlayClientEffectObjectMessage).
    [Preserve]
    internal sealed class PlayClientEffectObjectPacketHandler : GamePacketHandler<PlayClientEffectObjectPacket>
    {
        public override uint Opcode => PlayClientEffectObjectPacket.Opcode;

        [Preserve]
        public PlayClientEffectObjectPacketHandler() : base(PlayClientEffectObjectPacket.ReadFrom)
        {
        }

        protected override void Handle(PlayClientEffectObjectPacket packet)
        {
            throw new NotImplementedException($"PlayClientEffectObjectMessage (opcode 0x{Opcode:X8})");
        }
    }
}
