using Swg.Client.Runtime.Networking.Chat;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Chat;

namespace Swg.Client.Core.Networking.Game.Handlers.Chat
{
    // Core3/MMOCoreORB/src/server/zone/packets/chat/ChatInstantMessageToClient.h:10 (ChatInstantMessageToClient).
    [Preserve]
    internal sealed class ChatInstantMessageToClientPacketHandler : GamePacketHandler<ChatInstantMessageToClientPacket>
    {
        readonly ClientChat chat;
        public override uint Opcode => ChatInstantMessageToClientPacket.Opcode;

        [Preserve]
        public ChatInstantMessageToClientPacketHandler(ClientChat chat) : base(ChatInstantMessageToClientPacket.ReadFrom)
        {
            this.chat = chat;
        }

        protected override void Handle(ChatInstantMessageToClientPacket packet)
        {
            chat.Receive(packet);
        }
    }
}
