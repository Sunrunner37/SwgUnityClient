using Swg.Client.Runtime.Networking.Chat;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Chat;

namespace Swg.Client.Core.Networking.Game.Handlers.Chat
{
    // Core3/MMOCoreORB/src/server/zone/packets/chat/ChatOnEnteredRoom.h:10 (ChatOnEnteredRoom).
    [Preserve]
    internal sealed class ChatOnEnteredRoomPacketHandler : GamePacketHandler<ChatOnEnteredRoomPacket>
    {
        readonly ClientChat chat;
        public override uint Opcode => ChatOnEnteredRoomPacket.Opcode;

        [Preserve]
        public ChatOnEnteredRoomPacketHandler(ClientChat chat) : base(ChatOnEnteredRoomPacket.ReadFrom)
        {
            this.chat = chat;
        }

        protected override void Handle(ChatOnEnteredRoomPacket packet)
        {
            chat.Receive(packet);
        }
    }
}
