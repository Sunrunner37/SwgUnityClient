using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Chat;

namespace Swg.Client.Core.Networking.Game.Handlers.Chat
{
    // Core3/MMOCoreORB/src/server/zone/packets/chat/ChatQueryRoomResults.h:14 (ChatQueryRoomResults).
    [Preserve]
    internal sealed class ChatQueryRoomResultsPacketHandler : GamePacketHandler<ChatQueryRoomResultsPacket>
    {
        public override uint Opcode => ChatQueryRoomResultsPacket.Opcode;

        [Preserve]
        public ChatQueryRoomResultsPacketHandler() : base(ChatQueryRoomResultsPacket.ReadFrom)
        {
        }

        protected override void Handle(ChatQueryRoomResultsPacket packet)
        {
            throw new NotImplementedException($"ChatQueryRoomResults (opcode 0x{Opcode:X8})");
        }
    }
}
