using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Chat;

namespace Swg.Client.Core.Networking.Game.Handlers.Chat
{
    // Core3/MMOCoreORB/src/server/zone/packets/chat/ChatOnGetIgnoreList.h:11 (ChatOnGetIgnoreList).
    [Preserve]
    internal sealed class ChatOnGetIgnoreListPacketHandler : GamePacketHandler<ChatOnGetIgnoreListPacket>
    {
        public override uint Opcode => ChatOnGetIgnoreListPacket.Opcode;

        [Preserve]
        public ChatOnGetIgnoreListPacketHandler() : base(ChatOnGetIgnoreListPacket.ReadFrom)
        {
        }

        protected override void Handle(ChatOnGetIgnoreListPacket packet)
        {
            throw new NotImplementedException($"ChatOnGetIgnoreList (opcode 0x{Opcode:X8})");
        }
    }
}
