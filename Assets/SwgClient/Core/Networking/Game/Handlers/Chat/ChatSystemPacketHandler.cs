using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Chat;

namespace Swg.Client.Core.Networking.Game.Handlers.Chat
{
    // Core3/MMOCoreORB/src/server/zone/packets/chat/ChatSystemMessage.h:11 (ChatSystemMessage).
    [Preserve]
    internal sealed class ChatSystemPacketHandler : GamePacketHandler<ChatSystemPacket>
    {
        public override uint Opcode => ChatSystemPacket.Opcode;

        [Preserve]
        public ChatSystemPacketHandler() : base(ChatSystemPacket.ReadFrom)
        {
        }

        protected override void Handle(ChatSystemPacket packet)
        {
            throw new NotImplementedException($"ChatSystemMessage (opcode 0x{Opcode:X8})");
        }
    }
}
