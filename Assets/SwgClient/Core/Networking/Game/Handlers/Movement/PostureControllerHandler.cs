using Swg.Client.Core.Networking.Game.Packets.Movement;
using System;
using UnityEngine.Scripting;

namespace Swg.Client.Core.Networking.Game.Handlers.Movement
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/PostureMessage.h:10 (PostureMessage).
    [Preserve]
    internal sealed class PostureControllerHandler : ObjectControllerHandler<PosturePacket>
    {
        public override uint ControllerType => PosturePacket.ControllerType;

        [Preserve]
        public PostureControllerHandler() : base(PosturePacket.ReadFrom)
        {
        }

        protected override void Handle(PosturePacket packet)
        {
            throw new NotImplementedException($"PostureMessage (controller 0x{ControllerType:X8})");
        }
    }
}
