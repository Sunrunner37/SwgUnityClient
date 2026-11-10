using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Chat;

namespace Swg.Client.Core.Networking.Game.Handlers.Chat
{
    // Core3/MMOCoreORB/src/server/zone/packets/chat/ChatOnUnbanFromRoom.h:13 (ChatOnUnbanFromRoom).
    [Preserve]
    internal sealed class ChatOnUnbanFromRoomPacketHandler : GamePacketHandler<ChatOnUnbanFromRoomPacket>
    {
        public override uint Opcode => ChatOnUnbanFromRoomPacket.Opcode;

        [Preserve]
        public ChatOnUnbanFromRoomPacketHandler() : base(ChatOnUnbanFromRoomPacket.ReadFrom)
        {
        }

        protected override void Handle(ChatOnUnbanFromRoomPacket packet)
        {
            throw new NotImplementedException($"ChatOnUnbanFromRoom (opcode 0x{Opcode:X8})");
        }
    }
}
