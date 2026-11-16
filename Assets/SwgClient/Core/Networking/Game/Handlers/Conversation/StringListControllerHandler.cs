using Swg.Client.Core.Networking.Game.Packets.Conversation;
using Swg.Client.Runtime.Networking;
using UnityEngine.Scripting;

namespace Swg.Client.Core.Networking.Game.Handlers.Conversation
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/StringList.h:10 (StringList).
    [Preserve]
    internal sealed class StringListControllerHandler : ObjectControllerHandler<StringListPacket>
    {
        readonly ClientConversation conversation;
        public override uint ControllerType => StringListPacket.ControllerType;

        [Preserve]
        public StringListControllerHandler(ClientConversation conversation) : base(StringListPacket.ReadFrom)
        {
            this.conversation = conversation;
        }

        protected override void Handle(StringListPacket packet)
        {
            conversation.Receive(packet);
        }
    }
}
