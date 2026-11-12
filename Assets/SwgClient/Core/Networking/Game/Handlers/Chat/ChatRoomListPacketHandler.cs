using Swg.Client.Runtime.Networking.Chat;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Chat;

namespace Swg.Client.Core.Networking.Game.Handlers.Chat
{
    // Core3/MMOCoreORB/src/server/zone/packets/chat/ChatRoomList.h:17 (ChatRoomList).
    [Preserve]
    internal sealed class ChatRoomListPacketHandler : GamePacketHandler<ChatRoomListPacket>
    {
        readonly ClientChat chat;
        public override uint Opcode => ChatRoomListPacket.Opcode;

        [Preserve]
        public ChatRoomListPacketHandler(ClientChat chat) : base(ChatRoomListPacket.ReadFrom)
        {
            this.chat = chat;
        }

        protected override void Handle(ChatRoomListPacket packet)
        {
            chat.Receive(packet);
        }
    }
}
