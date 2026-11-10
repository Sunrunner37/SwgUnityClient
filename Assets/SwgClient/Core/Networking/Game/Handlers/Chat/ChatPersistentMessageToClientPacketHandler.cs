using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Chat;

namespace Swg.Client.Core.Networking.Game.Handlers.Chat
{
    // Core3/MMOCoreORB/src/server/zone/packets/chat/ChatPersistentMessageToClient.h:13 (ChatPersistentMessageToClient).
    [Preserve]
    internal sealed class ChatPersistentMessageToClientPacketHandler : GamePacketHandler<ChatPersistentMessageToClientPacket>
    {
        public override uint Opcode => ChatPersistentMessageToClientPacket.Opcode;

        [Preserve]
        public ChatPersistentMessageToClientPacketHandler() : base(ChatPersistentMessageToClientPacket.ReadFrom)
        {
        }

        protected override void Handle(ChatPersistentMessageToClientPacket packet)
        {
            throw new NotImplementedException($"ChatPersistentMessageToClient (opcode 0x{Opcode:X8})");
        }
    }
}
