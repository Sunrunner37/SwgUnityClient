using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Chat;

namespace Swg.Client.Core.Networking.Game.Handlers.Chat
{
    // Core3/MMOCoreORB/src/server/zone/packets/chat/ChatOnRemoveModeratorFromRoom.h:13 (ChatOnRemoveModeratorFromRoom).
    [Preserve]
    internal sealed class ChatOnRemoveModeratorFromRoomPacketHandler : GamePacketHandler<ChatOnRemoveModeratorFromRoomPacket>
    {
        public override uint Opcode => ChatOnRemoveModeratorFromRoomPacket.Opcode;

        [Preserve]
        public ChatOnRemoveModeratorFromRoomPacketHandler() : base(ChatOnRemoveModeratorFromRoomPacket.ReadFrom)
        {
        }

        protected override void Handle(ChatOnRemoveModeratorFromRoomPacket packet)
        {
            throw new NotImplementedException($"ChatOnRemoveModeratorFromRoom (opcode 0x{Opcode:X8})");
        }
    }
}
