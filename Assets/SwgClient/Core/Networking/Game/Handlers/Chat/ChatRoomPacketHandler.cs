using Swg.Client.Runtime.Networking.Chat;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Chat;

namespace Swg.Client.Core.Networking.Game.Handlers.Chat
{
    // Core3/MMOCoreORB/src/server/zone/packets/chat/ChatRoomMessage.h:10 (ChatRoomMessage).
    [Preserve]
    internal sealed class ChatRoomPacketHandler : GamePacketHandler<ChatRoomPacket>
    {
        readonly ClientChat chat;
        public override uint Opcode => ChatRoomPacket.Opcode;

        [Preserve]
        public ChatRoomPacketHandler(ClientChat chat) : base(ChatRoomPacket.ReadFrom)
        {
            this.chat = chat;
        }

        protected override void Handle(ChatRoomPacket packet)
        {
            chat.Receive(packet);
        }
    }
}
