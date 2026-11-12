using Swg.Client.Runtime.Networking.Chat;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Chat;

namespace Swg.Client.Core.Networking.Game.Handlers.Chat
{
    // Core3/MMOCoreORB/src/server/zone/packets/chat/ChatOnCreateRoom.h:11 (ChatOnCreateRoom).
    [Preserve]
    internal sealed class ChatOnCreateRoomPacketHandler : GamePacketHandler<ChatOnCreateRoomPacket>
    {
        readonly ClientChat chat;
        public override uint Opcode => ChatOnCreateRoomPacket.Opcode;

        [Preserve]
        public ChatOnCreateRoomPacketHandler(ClientChat chat) : base(ChatOnCreateRoomPacket.ReadFrom)
        {
            this.chat = chat;
        }

        protected override void Handle(ChatOnCreateRoomPacket packet)
        {
            chat.Receive(packet);
        }
    }
}
