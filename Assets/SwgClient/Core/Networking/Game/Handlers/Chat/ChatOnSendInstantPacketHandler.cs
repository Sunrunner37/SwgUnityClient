using Swg.Client.Runtime.Networking.Chat;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Chat;

namespace Swg.Client.Core.Networking.Game.Handlers.Chat
{
    // Core3/MMOCoreORB/src/server/zone/packets/chat/ChatOnSendInstantMessage.h:10 (ChatOnSendInstantMessage).
    [Preserve]
    internal sealed class ChatOnSendInstantPacketHandler : GamePacketHandler<ChatOnSendInstantPacket>
    {
        readonly ClientChat chat;
        public override uint Opcode => ChatOnSendInstantPacket.Opcode;

        [Preserve]
        public ChatOnSendInstantPacketHandler(ClientChat chat) : base(ChatOnSendInstantPacket.ReadFrom)
        {
            this.chat = chat;
        }

        protected override void Handle(ChatOnSendInstantPacket packet)
        {
            chat.Receive(packet);
        }
    }
}
