using Swg.Client.Runtime.Networking.Chat;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Chat;

namespace Swg.Client.Core.Networking.Game.Handlers.Chat
{
    // Core3/MMOCoreORB/src/server/zone/packets/chat/ChatSystemMessage.h:11 (ChatSystemMessage).
    [Preserve]
    internal sealed class ChatSystemPacketHandler : GamePacketHandler<ChatSystemPacket>
    {
        readonly ClientChat chat;
        public override uint Opcode => ChatSystemPacket.Opcode;

        [Preserve]
        public ChatSystemPacketHandler(ClientChat chat) : base(ChatSystemPacket.ReadFrom)
        {
            this.chat = chat;
        }

        protected override void Handle(ChatSystemPacket packet)
        {
            chat.Receive(packet);
        }
    }
}
