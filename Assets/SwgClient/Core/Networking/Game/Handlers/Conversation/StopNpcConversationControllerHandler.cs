using Swg.Client.Core.Networking.Game.Packets.Conversation;
using System;
using UnityEngine.Scripting;

namespace Swg.Client.Core.Networking.Game.Handlers.Conversation
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/StopNpcConversation.h:8 (StopNpcConversation).
    [Preserve]
    internal sealed class StopNpcConversationControllerHandler : ObjectControllerHandler<StopNpcConversationPacket>
    {
        public override uint ControllerType => StopNpcConversationPacket.ControllerType;

        [Preserve]
        public StopNpcConversationControllerHandler() : base(StopNpcConversationPacket.ReadFrom)
        {
        }

        protected override void Handle(StopNpcConversationPacket packet)
        {
            throw new NotImplementedException($"StopNpcConversation (controller 0x{ControllerType:X8})");
        }
    }
}
