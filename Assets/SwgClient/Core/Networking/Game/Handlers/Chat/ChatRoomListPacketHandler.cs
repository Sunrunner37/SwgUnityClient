using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Chat;

namespace Swg.Client.Core.Networking.Game.Handlers.Chat
{
    // Core3/MMOCoreORB/src/server/zone/packets/chat/ChatRoomList.h:17 (ChatRoomList).
    [Preserve]
    internal sealed class ChatRoomListPacketHandler : GamePacketHandler<ChatRoomListPacket>
    {
        public override uint Opcode => ChatRoomListPacket.Opcode;

        [Preserve]
        public ChatRoomListPacketHandler() : base(ChatRoomListPacket.ReadFrom)
        {
        }

        protected override void Handle(ChatRoomListPacket packet)
        {
            throw new NotImplementedException($"ChatRoomList (opcode 0x{Opcode:X8})");
        }
    }
}
