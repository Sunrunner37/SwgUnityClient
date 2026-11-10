using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Chat;

namespace Swg.Client.Core.Networking.Game.Handlers.Chat
{
    // Core3/MMOCoreORB/src/server/zone/packets/chat/ChatOnAddModeratorToRoom.h:13 (ChatOnAddModeratorToRoom).
    [Preserve]
    internal sealed class ChatOnAddModeratorToRoomPacketHandler : GamePacketHandler<ChatOnAddModeratorToRoomPacket>
    {
        public override uint Opcode => ChatOnAddModeratorToRoomPacket.Opcode;

        [Preserve]
        public ChatOnAddModeratorToRoomPacketHandler() : base(ChatOnAddModeratorToRoomPacket.ReadFrom)
        {
        }

        protected override void Handle(ChatOnAddModeratorToRoomPacket packet)
        {
            throw new NotImplementedException($"ChatOnAddModeratorToRoom (opcode 0x{Opcode:X8})");
        }
    }
}
