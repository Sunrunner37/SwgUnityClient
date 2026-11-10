using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Chat;

namespace Swg.Client.Core.Networking.Game.Handlers.Chat
{
    // Core3/MMOCoreORB/src/server/zone/packets/chat/ChatOnBanFromRoom.h:13 (ChatOnBanFromRoom).
    [Preserve]
    internal sealed class ChatOnBanFromRoomPacketHandler : GamePacketHandler<ChatOnBanFromRoomPacket>
    {
        public override uint Opcode => ChatOnBanFromRoomPacket.Opcode;

        [Preserve]
        public ChatOnBanFromRoomPacketHandler() : base(ChatOnBanFromRoomPacket.ReadFrom)
        {
        }

        protected override void Handle(ChatOnBanFromRoomPacket packet)
        {
            throw new NotImplementedException($"ChatOnBanFromRoom (opcode 0x{Opcode:X8})");
        }
    }
}
