using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Chat;

namespace Swg.Client.Core.Networking.Game.Handlers.Chat
{
    // Core3/MMOCoreORB/src/server/zone/packets/chat/ChatOnSendInstantMessage.h:10 (ChatOnSendInstantMessage).
    [Preserve]
    internal sealed class ChatOnSendInstantPacketHandler : GamePacketHandler<ChatOnSendInstantPacket>
    {
        public override uint Opcode => ChatOnSendInstantPacket.Opcode;

        [Preserve]
        public ChatOnSendInstantPacketHandler() : base(ChatOnSendInstantPacket.ReadFrom)
        {
        }

        protected override void Handle(ChatOnSendInstantPacket packet)
        {
            throw new NotImplementedException($"ChatOnSendInstantMessage (opcode 0x{Opcode:X8})");
        }
    }
}
