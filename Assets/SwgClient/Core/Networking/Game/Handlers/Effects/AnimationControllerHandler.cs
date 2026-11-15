using Swg.Client.Core.Networking.Game.Packets.Effects;
using Swg.Client.Runtime.Networking;
using UnityEngine.Scripting;

namespace Swg.Client.Core.Networking.Game.Handlers.Effects
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/Animation.h:10 (Animation).
    [Preserve]
    internal sealed class AnimationControllerHandler : ObjectControllerHandler<AnimationPacket>
    {
        public override uint ControllerType => AnimationPacket.ControllerType;

        readonly ClientCombat combat;
        [Preserve]
        public AnimationControllerHandler(ClientCombat combat) : base(AnimationPacket.ReadFrom) => this.combat = combat;
        protected override void Handle(AnimationPacket packet) => combat.Receive(packet);
    }
}
