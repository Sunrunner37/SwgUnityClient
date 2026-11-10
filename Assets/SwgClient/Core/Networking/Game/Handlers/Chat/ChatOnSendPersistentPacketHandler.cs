using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Chat;

namespace Swg.Client.Core.Networking.Game.Handlers.Chat
{
    // Core3/MMOCoreORB/src/server/zone/packets/chat/ChatOnSendPersistentMessage.h:12 (ChatOnSendPersistentMessage).
    [Preserve]
    internal sealed class ChatOnSendPersistentPacketHandler : GamePacketHandler<ChatOnSendPersistentPacket>
    {
        public override uint Opcode => ChatOnSendPersistentPacket.Opcode;

        [Preserve]
        public ChatOnSendPersistentPacketHandler() : base(ChatOnSendPersistentPacket.ReadFrom)
        {
        }

        protected override void Handle(ChatOnSendPersistentPacket packet)
        {
            throw new NotImplementedException($"ChatOnSendPersistentMessage (opcode 0x{Opcode:X8})");
        }
    }
}
