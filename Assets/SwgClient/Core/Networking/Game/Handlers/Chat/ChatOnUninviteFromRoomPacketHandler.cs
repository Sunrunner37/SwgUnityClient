using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Chat;

namespace Swg.Client.Core.Networking.Game.Handlers.Chat
{
    // Core3/MMOCoreORB/src/server/zone/packets/chat/ChatOnUninviteFromRoom.h:13 (ChatOnUninviteFromRoom).
    [Preserve]
    internal sealed class ChatOnUninviteFromRoomPacketHandler : GamePacketHandler<ChatOnUninviteFromRoomPacket>
    {
        public override uint Opcode => ChatOnUninviteFromRoomPacket.Opcode;

        [Preserve]
        public ChatOnUninviteFromRoomPacketHandler() : base(ChatOnUninviteFromRoomPacket.ReadFrom)
        {
        }

        protected override void Handle(ChatOnUninviteFromRoomPacket packet)
        {
            throw new NotImplementedException($"ChatOnUninviteFromRoom (opcode 0x{Opcode:X8})");
        }
    }
}
