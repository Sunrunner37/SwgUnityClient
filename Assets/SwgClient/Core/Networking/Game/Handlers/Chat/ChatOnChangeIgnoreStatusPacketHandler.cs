using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Chat;

namespace Swg.Client.Core.Networking.Game.Handlers.Chat
{
    // Core3/MMOCoreORB/src/server/zone/packets/chat/ChatOnChangeIgnoreStatus.h:10 (ChatOnChangeIgnoreStatus).
    [Preserve]
    internal sealed class ChatOnChangeIgnoreStatusPacketHandler : GamePacketHandler<ChatOnChangeIgnoreStatusPacket>
    {
        public override uint Opcode => ChatOnChangeIgnoreStatusPacket.Opcode;

        [Preserve]
        public ChatOnChangeIgnoreStatusPacketHandler() : base(ChatOnChangeIgnoreStatusPacket.ReadFrom)
        {
        }

        protected override void Handle(ChatOnChangeIgnoreStatusPacket packet)
        {
            throw new NotImplementedException($"ChatOnChangeIgnoreStatus (opcode 0x{Opcode:X8})");
        }
    }
}
