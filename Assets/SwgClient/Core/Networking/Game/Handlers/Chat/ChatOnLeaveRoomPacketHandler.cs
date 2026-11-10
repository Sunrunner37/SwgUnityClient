using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Chat;

namespace Swg.Client.Core.Networking.Game.Handlers.Chat
{
    // Core3/MMOCoreORB/src/server/zone/packets/chat/ChatOnLeaveRoom.h:12 (ChatOnLeaveRoom).
    [Preserve]
    internal sealed class ChatOnLeaveRoomPacketHandler : GamePacketHandler<ChatOnLeaveRoomPacket>
    {
        public override uint Opcode => ChatOnLeaveRoomPacket.Opcode;

        [Preserve]
        public ChatOnLeaveRoomPacketHandler() : base(ChatOnLeaveRoomPacket.ReadFrom)
        {
        }

        protected override void Handle(ChatOnLeaveRoomPacket packet)
        {
            throw new NotImplementedException($"ChatOnLeaveRoom (opcode 0x{Opcode:X8})");
        }
    }
}
