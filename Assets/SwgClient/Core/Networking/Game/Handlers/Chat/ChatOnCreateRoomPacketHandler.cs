using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Chat;

namespace Swg.Client.Core.Networking.Game.Handlers.Chat
{
    // Core3/MMOCoreORB/src/server/zone/packets/chat/ChatOnCreateRoom.h:11 (ChatOnCreateRoom).
    [Preserve]
    internal sealed class ChatOnCreateRoomPacketHandler : GamePacketHandler<ChatOnCreateRoomPacket>
    {
        public override uint Opcode => ChatOnCreateRoomPacket.Opcode;

        [Preserve]
        public ChatOnCreateRoomPacketHandler() : base(ChatOnCreateRoomPacket.ReadFrom)
        {
        }

        protected override void Handle(ChatOnCreateRoomPacket packet)
        {
            throw new NotImplementedException($"ChatOnCreateRoom (opcode 0x{Opcode:X8})");
        }
    }
}
