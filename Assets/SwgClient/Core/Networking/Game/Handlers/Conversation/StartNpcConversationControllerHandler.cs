using Swg.Client.Core.Networking.Game.Packets.Conversation;
using System;
using UnityEngine.Scripting;

namespace Swg.Client.Core.Networking.Game.Handlers.Conversation
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/StartNpcConversation.h:10 (StartNpcConversation).
    [Preserve]
    internal sealed class StartNpcConversationControllerHandler : ObjectControllerHandler<StartNpcConversationPacket>
    {
        public override uint ControllerType => StartNpcConversationPacket.ControllerType;

        [Preserve]
        public StartNpcConversationControllerHandler() : base(StartNpcConversationPacket.ReadFrom)
        {
        }

        protected override void Handle(StartNpcConversationPacket packet)
        {
            throw new NotImplementedException($"StartNpcConversation (controller 0x{ControllerType:X8})");
        }
    }
}
