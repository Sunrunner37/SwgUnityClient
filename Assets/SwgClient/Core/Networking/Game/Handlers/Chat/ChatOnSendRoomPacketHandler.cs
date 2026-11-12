using Swg.Client.Runtime.Networking.Chat;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Chat;

namespace Swg.Client.Core.Networking.Game.Handlers.Chat
{
    // Core3/MMOCoreORB/src/server/zone/packets/chat/ChatOnSendRoomMessage.h:10 (ChatOnSendRoomMessage).
    [Preserve]
    internal sealed class ChatOnSendRoomPacketHandler : GamePacketHandler<ChatOnSendRoomPacket>
    {
        readonly ClientChat chat;
        public override uint Opcode => ChatOnSendRoomPacket.Opcode;

        [Preserve]
        public ChatOnSendRoomPacketHandler(ClientChat chat) : base(ChatOnSendRoomPacket.ReadFrom)
        {
            this.chat = chat;
        }

        protected override void Handle(ChatOnSendRoomPacket packet)
        {
            chat.Receive(packet);
        }
    }
}
