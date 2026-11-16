using Swg.Client.Core.Networking.Game.Packets.Conversation;
using Swg.Client.Runtime.Networking;
using UnityEngine.Scripting;

namespace Swg.Client.Core.Networking.Game.Handlers.Conversation
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/NpcConversationMessage.h:14 (NpcConversationMessage).
    [Preserve]
    internal sealed class NpcConversationControllerHandler : ObjectControllerHandler<NpcConversationPacket>
    {
        readonly ClientConversation conversation;
        public override uint ControllerType => NpcConversationPacket.ControllerType;

        [Preserve]
        public NpcConversationControllerHandler(ClientConversation conversation) : base(NpcConversationPacket.ReadFrom)
        {
            this.conversation = conversation;
        }

        protected override void Handle(NpcConversationPacket packet)
        {
            conversation.Receive(packet);
        }
    }
}
