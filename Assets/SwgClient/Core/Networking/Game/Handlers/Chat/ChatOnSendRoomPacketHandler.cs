using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Chat;

namespace Swg.Client.Core.Networking.Game.Handlers.Chat
{
    // Core3/MMOCoreORB/src/server/zone/packets/chat/ChatOnSendRoomMessage.h:10 (ChatOnSendRoomMessage).
    [Preserve]
    internal sealed class ChatOnSendRoomPacketHandler : GamePacketHandler<ChatOnSendRoomPacket>
    {
        public override uint Opcode => ChatOnSendRoomPacket.Opcode;

        [Preserve]
        public ChatOnSendRoomPacketHandler() : base(ChatOnSendRoomPacket.ReadFrom)
        {
        }

        protected override void Handle(ChatOnSendRoomPacket packet)
        {
            throw new NotImplementedException($"ChatOnSendRoomMessage (opcode 0x{Opcode:X8})");
        }
    }
}
