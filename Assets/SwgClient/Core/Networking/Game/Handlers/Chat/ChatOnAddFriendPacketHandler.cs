using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Chat;

namespace Swg.Client.Core.Networking.Game.Handlers.Chat
{
    // Core3/MMOCoreORB/src/server/zone/packets/chat/ChatOnAddFriend.h:10 (ChatOnAddFriend).
    [Preserve]
    internal sealed class ChatOnAddFriendPacketHandler : GamePacketHandler<ChatOnAddFriendPacket>
    {
        public override uint Opcode => ChatOnAddFriendPacket.Opcode;

        [Preserve]
        public ChatOnAddFriendPacketHandler() : base(ChatOnAddFriendPacket.ReadFrom)
        {
        }

        protected override void Handle(ChatOnAddFriendPacket packet)
        {
            throw new NotImplementedException($"ChatOnAddFriend (opcode 0x{Opcode:X8})");
        }
    }
}
