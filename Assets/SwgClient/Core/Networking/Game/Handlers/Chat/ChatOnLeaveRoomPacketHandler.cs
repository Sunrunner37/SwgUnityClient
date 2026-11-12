using Swg.Client.Runtime.Networking.Chat;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Chat;

namespace Swg.Client.Core.Networking.Game.Handlers.Chat
{
    // Core3/MMOCoreORB/src/server/zone/packets/chat/ChatOnLeaveRoom.h:12 (ChatOnLeaveRoom).
    [Preserve]
    internal sealed class ChatOnLeaveRoomPacketHandler : GamePacketHandler<ChatOnLeaveRoomPacket>
    {
        readonly ClientChat chat;
        public override uint Opcode => ChatOnLeaveRoomPacket.Opcode;

        [Preserve]
        public ChatOnLeaveRoomPacketHandler(ClientChat chat) : base(ChatOnLeaveRoomPacket.ReadFrom)
        {
            this.chat = chat;
        }

        protected override void Handle(ChatOnLeaveRoomPacket packet)
        {
            chat.Receive(packet);
        }
    }
}
