using Swg.Client.Core.Networking.Game.Packets.Conversation;
using Swg.Client.Runtime.Networking;
using UnityEngine.Scripting;

namespace Swg.Client.Core.Networking.Game.Handlers.Conversation
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/StartNpcConversation.h:10 (StartNpcConversation).
    [Preserve]
    internal sealed class StartNpcConversationControllerHandler : ObjectControllerHandler<StartNpcConversationPacket>
    {
        readonly ClientConversation conversation;
        public override uint ControllerType => StartNpcConversationPacket.ControllerType;

        [Preserve]
        public StartNpcConversationControllerHandler(ClientConversation conversation) : base(StartNpcConversationPacket.ReadFrom)
        {
            this.conversation = conversation;
        }

        protected override void Handle(StartNpcConversationPacket packet)
        {
            conversation.Receive(packet);
        }
    }
}
