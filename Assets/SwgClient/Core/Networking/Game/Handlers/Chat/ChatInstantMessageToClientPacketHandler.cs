using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Chat;

namespace Swg.Client.Core.Networking.Game.Handlers.Chat
{
    // Core3/MMOCoreORB/src/server/zone/packets/chat/ChatInstantMessageToClient.h:10 (ChatInstantMessageToClient).
    [Preserve]
    internal sealed class ChatInstantMessageToClientPacketHandler : GamePacketHandler<ChatInstantMessageToClientPacket>
    {
        public override uint Opcode => ChatInstantMessageToClientPacket.Opcode;

        [Preserve]
        public ChatInstantMessageToClientPacketHandler() : base(ChatInstantMessageToClientPacket.ReadFrom)
        {
        }

        protected override void Handle(ChatInstantMessageToClientPacket packet)
        {
            throw new NotImplementedException($"ChatInstantMessageToClient (opcode 0x{Opcode:X8})");
        }
    }
}
