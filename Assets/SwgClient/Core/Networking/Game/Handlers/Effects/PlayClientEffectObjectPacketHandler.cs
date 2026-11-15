using Swg.Client.Runtime.Assets.Particles;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Effects;

namespace Swg.Client.Core.Networking.Game.Handlers.Effects
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/PlayClientEffectObjectMessage.h:12 (PlayClientEffectObjectMessage).
    [Preserve]
    internal sealed class PlayClientEffectObjectPacketHandler : GamePacketHandler<PlayClientEffectObjectPacket>
    {
        public override uint Opcode => PlayClientEffectObjectPacket.Opcode;

        readonly ClientEffects effects;
        [Preserve]
        public PlayClientEffectObjectPacketHandler(ClientEffects effects) : base(PlayClientEffectObjectPacket.ReadFrom) => this.effects = effects;
        protected override void Handle(PlayClientEffectObjectPacket packet) => effects.Receive(packet);
    }
}
