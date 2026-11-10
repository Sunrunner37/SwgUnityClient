using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Chat;

namespace Swg.Client.Core.Networking.Game.Handlers.Chat
{
    // Core3/MMOCoreORB/src/server/zone/packets/chat/ChatOnInviteToRoom.h:13 (ChatOnInviteToRoom).
    [Preserve]
    internal sealed class ChatOnInviteToRoomPacketHandler : GamePacketHandler<ChatOnInviteToRoomPacket>
    {
        public override uint Opcode => ChatOnInviteToRoomPacket.Opcode;

        [Preserve]
        public ChatOnInviteToRoomPacketHandler() : base(ChatOnInviteToRoomPacket.ReadFrom)
        {
        }

        protected override void Handle(ChatOnInviteToRoomPacket packet)
        {
            throw new NotImplementedException($"ChatOnInviteToRoom (opcode 0x{Opcode:X8})");
        }
    }
}
