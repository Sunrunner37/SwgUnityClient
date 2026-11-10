using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Chat;

namespace Swg.Client.Core.Networking.Game.Handlers.Chat
{
    // Core3/MMOCoreORB/src/server/zone/packets/chat/ChatFriendsListUpdate.h:10 (ChatFriendsListUpdate).
    [Preserve]
    internal sealed class ChatFriendsListUpdatePacketHandler : GamePacketHandler<ChatFriendsListUpdatePacket>
    {
        public override uint Opcode => ChatFriendsListUpdatePacket.Opcode;

        [Preserve]
        public ChatFriendsListUpdatePacketHandler() : base(ChatFriendsListUpdatePacket.ReadFrom)
        {
        }

        protected override void Handle(ChatFriendsListUpdatePacket packet)
        {
            throw new NotImplementedException($"ChatFriendsListUpdate (opcode 0x{Opcode:X8})");
        }
    }
}
