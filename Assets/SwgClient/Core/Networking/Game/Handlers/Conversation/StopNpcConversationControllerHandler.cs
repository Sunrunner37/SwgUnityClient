using Swg.Client.Core.Networking.Game.Packets.Conversation;
using Swg.Client.Runtime.Networking;
using UnityEngine.Scripting;

namespace Swg.Client.Core.Networking.Game.Handlers.Conversation
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/StopNpcConversation.h:8 (StopNpcConversation).
    [Preserve]
    internal sealed class StopNpcConversationControllerHandler : ObjectControllerHandler<StopNpcConversationPacket>
    {
        readonly ClientConversation conversation;
        public override uint ControllerType => StopNpcConversationPacket.ControllerType;

        [Preserve]
        public StopNpcConversationControllerHandler(ClientConversation conversation) : base(StopNpcConversationPacket.ReadFrom)
        {
            this.conversation = conversation;
        }

        protected override void Handle(StopNpcConversationPacket packet)
        {
            conversation.Receive(packet);
        }
    }
}
