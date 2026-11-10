using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Chat;

namespace Swg.Client.Core.Networking.Game.Handlers.Chat
{
    // Core3/MMOCoreORB/src/server/zone/packets/chat/ChatOnChangeFriendStatus.h:10 (ChatOnChangeFriendStatus).
    [Preserve]
    internal sealed class ChatOnChangeFriendStatusPacketHandler : GamePacketHandler<ChatOnChangeFriendStatusPacket>
    {
        public override uint Opcode => ChatOnChangeFriendStatusPacket.Opcode;

        [Preserve]
        public ChatOnChangeFriendStatusPacketHandler() : base(ChatOnChangeFriendStatusPacket.ReadFrom)
        {
        }

        protected override void Handle(ChatOnChangeFriendStatusPacket packet)
        {
            throw new NotImplementedException($"ChatOnChangeFriendStatus (opcode 0x{Opcode:X8})");
        }
    }
}
