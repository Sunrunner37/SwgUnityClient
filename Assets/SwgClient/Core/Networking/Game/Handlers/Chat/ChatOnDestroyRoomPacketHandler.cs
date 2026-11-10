using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Chat;

namespace Swg.Client.Core.Networking.Game.Handlers.Chat
{
    // Core3/MMOCoreORB/src/server/zone/packets/chat/ChatOnDestroyRoom.h:8 (ChatOnDestroyRoom).
    [Preserve]
    internal sealed class ChatOnDestroyRoomPacketHandler : GamePacketHandler<ChatOnDestroyRoomPacket>
    {
        public override uint Opcode => ChatOnDestroyRoomPacket.Opcode;

        [Preserve]
        public ChatOnDestroyRoomPacketHandler() : base(ChatOnDestroyRoomPacket.ReadFrom)
        {
        }

        protected override void Handle(ChatOnDestroyRoomPacket packet)
        {
            throw new NotImplementedException($"ChatOnDestroyRoom (opcode 0x{Opcode:X8})");
        }
    }
}
