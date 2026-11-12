using Swg.Client.Runtime.Networking.Chat;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Chat;

namespace Swg.Client.Core.Networking.Game.Handlers.Chat
{
    // Core3/MMOCoreORB/src/server/zone/packets/chat/ChatQueryRoomResults.h:14 (ChatQueryRoomResults).
    [Preserve]
    internal sealed class ChatQueryRoomResultsPacketHandler : GamePacketHandler<ChatQueryRoomResultsPacket>
    {
        readonly ClientChat chat;
        public override uint Opcode => ChatQueryRoomResultsPacket.Opcode;

        [Preserve]
        public ChatQueryRoomResultsPacketHandler(ClientChat chat) : base(ChatQueryRoomResultsPacket.ReadFrom)
        {
            this.chat = chat;
        }

        protected override void Handle(ChatQueryRoomResultsPacket packet)
        {
            chat.Receive(packet);
        }
    }
}
