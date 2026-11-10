using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Chat;

namespace Swg.Client.Core.Networking.Game.Handlers.Chat
{
    // Core3/MMOCoreORB/src/server/zone/packets/chat/ChatRoomMessage.h:10 (ChatRoomMessage).
    [Preserve]
    internal sealed class ChatRoomPacketHandler : GamePacketHandler<ChatRoomPacket>
    {
        public override uint Opcode => ChatRoomPacket.Opcode;

        [Preserve]
        public ChatRoomPacketHandler() : base(ChatRoomPacket.ReadFrom)
        {
        }

        protected override void Handle(ChatRoomPacket packet)
        {
            throw new NotImplementedException($"ChatRoomMessage (opcode 0x{Opcode:X8})");
        }
    }
}
