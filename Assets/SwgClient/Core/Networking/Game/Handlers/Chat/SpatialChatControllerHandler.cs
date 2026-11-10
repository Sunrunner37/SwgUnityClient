using Swg.Client.Core.Networking.Game.Packets.Chat;
using System;
using UnityEngine.Scripting;

namespace Swg.Client.Core.Networking.Game.Handlers.Chat
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/SpatialChat.h:12 (SpatialChat).
    [Preserve]
    internal sealed class SpatialChatControllerHandler : ObjectControllerHandler<SpatialChatPacket>
    {
        public override uint ControllerType => SpatialChatPacket.ControllerType;

        [Preserve]
        public SpatialChatControllerHandler() : base(SpatialChatPacket.ReadFrom)
        {
        }

        protected override void Handle(SpatialChatPacket packet)
        {
            throw new NotImplementedException($"SpatialChat (controller 0x{ControllerType:X8})");
        }
    }
}
