using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Chat;

namespace Swg.Client.Core.Networking.Game.Handlers.Chat
{
    // Core3/MMOCoreORB/src/server/zone/packets/chat/ChatOnEnteredRoom.h:10 (ChatOnEnteredRoom).
    [Preserve]
    internal sealed class ChatOnEnteredRoomPacketHandler : GamePacketHandler<ChatOnEnteredRoomPacket>
    {
        public override uint Opcode => ChatOnEnteredRoomPacket.Opcode;

        [Preserve]
        public ChatOnEnteredRoomPacketHandler() : base(ChatOnEnteredRoomPacket.ReadFrom)
        {
        }

        protected override void Handle(ChatOnEnteredRoomPacket packet)
        {
            throw new NotImplementedException($"ChatOnEnteredRoom (opcode 0x{Opcode:X8})");
        }
    }
}
