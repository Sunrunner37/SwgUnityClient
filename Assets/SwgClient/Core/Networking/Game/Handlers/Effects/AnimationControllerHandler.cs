using Swg.Client.Core.Networking.Game.Packets.Effects;
using System;
using UnityEngine.Scripting;

namespace Swg.Client.Core.Networking.Game.Handlers.Effects
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/Animation.h:10 (Animation).
    [Preserve]
    internal sealed class AnimationControllerHandler : ObjectControllerHandler<AnimationPacket>
    {
        public override uint ControllerType => AnimationPacket.ControllerType;

        [Preserve]
        public AnimationControllerHandler() : base(AnimationPacket.ReadFrom)
        {
        }

        protected override void Handle(AnimationPacket packet)
        {
            throw new NotImplementedException($"Animation (controller 0x{ControllerType:X8})");
        }
    }
}
