using Swg.Client.Runtime.Networking.Chat;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Chat;

namespace Swg.Client.Core.Networking.Game.Handlers.Chat
{
    // Core3/MMOCoreORB/src/server/zone/packets/chat/ChatOnDestroyRoom.h:8 (ChatOnDestroyRoom).
    [Preserve]
    internal sealed class ChatOnDestroyRoomPacketHandler : GamePacketHandler<ChatOnDestroyRoomPacket>
    {
        readonly ClientChat chat;
        public override uint Opcode => ChatOnDestroyRoomPacket.Opcode;

        [Preserve]
        public ChatOnDestroyRoomPacketHandler(ClientChat chat) : base(ChatOnDestroyRoomPacket.ReadFrom)
        {
            this.chat = chat;
        }

        protected override void Handle(ChatOnDestroyRoomPacket packet)
        {
            chat.Receive(packet);
        }
    }
}
