using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Chat;

namespace Swg.Client.Core.Networking.Game.Handlers.Chat
{
    // Core3/MMOCoreORB/src/server/zone/packets/chat/ChatServerStatus.h:13 (ChatServerStatus).
    // Core3/MMOCoreORB/src/server/zone/packets/zone/unkByteFlag.h:10 (unkByteFlag).
    [Preserve]
    internal sealed class ChatServerStatusPacketHandler : GamePacketHandler<ChatServerStatusPacket>
    {
        public override uint Opcode => ChatServerStatusPacket.Opcode;

        [Preserve]
        public ChatServerStatusPacketHandler() : base(ChatServerStatusPacket.ReadFrom)
        {
        }

        protected override void Handle(ChatServerStatusPacket packet)
        {
            throw new NotImplementedException($"ChatServerStatus (opcode 0x{Opcode:X8})");
        }
    }
}
