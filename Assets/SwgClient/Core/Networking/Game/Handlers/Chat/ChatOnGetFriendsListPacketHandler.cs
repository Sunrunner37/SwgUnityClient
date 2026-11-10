using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Chat;

namespace Swg.Client.Core.Networking.Game.Handlers.Chat
{
    // Core3/MMOCoreORB/src/server/zone/packets/chat/ChatOnGetFriendsList.h:11 (ChatOnGetFriendsList).
    [Preserve]
    internal sealed class ChatOnGetFriendsListPacketHandler : GamePacketHandler<ChatOnGetFriendsListPacket>
    {
        public override uint Opcode => ChatOnGetFriendsListPacket.Opcode;

        [Preserve]
        public ChatOnGetFriendsListPacketHandler() : base(ChatOnGetFriendsListPacket.ReadFrom)
        {
        }

        protected override void Handle(ChatOnGetFriendsListPacket packet)
        {
            throw new NotImplementedException($"ChatOnGetFriendsList (opcode 0x{Opcode:X8})");
        }
    }
}
