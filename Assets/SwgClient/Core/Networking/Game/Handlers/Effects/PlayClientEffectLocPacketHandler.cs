using Swg.Client.Core.Networking.Game.Packets.Effects;
using Swg.Client.Runtime.Assets.Particles;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Scene;

namespace Swg.Client.Core.Networking.Game.Handlers.Effects
{
    // Core3/MMOCoreORB/src/server/zone/packets/scene/PlayClientEffectLocMessage.h:10 (PlayClientEffectLoc).
    [Preserve]
    internal sealed class PlayClientEffectLocPacketHandler : GamePacketHandler<PlayClientEffectLocPacket>
    {
        public override uint Opcode => PlayClientEffectLocPacket.Opcode;

        readonly ClientEffects effects;
        [Preserve]
        public PlayClientEffectLocPacketHandler(ClientEffects effects) : base(PlayClientEffectLocPacket.ReadFrom) => this.effects = effects;
        protected override void Handle(PlayClientEffectLocPacket packet) => effects.Receive(packet);
    }
}
