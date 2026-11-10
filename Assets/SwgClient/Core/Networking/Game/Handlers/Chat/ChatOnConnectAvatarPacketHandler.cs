using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Chat;

namespace Swg.Client.Core.Networking.Game.Handlers.Chat
{
    // Core3/MMOCoreORB/src/server/zone/packets/chat/ChatOnConnectAvatar.h:13 (ChatOnConnectAvatar).
    [Preserve]
    internal sealed class ChatOnConnectAvatarPacketHandler : GamePacketHandler<ChatOnConnectAvatarPacket>
    {
        public override uint Opcode => ChatOnConnectAvatarPacket.Opcode;

        [Preserve]
        public ChatOnConnectAvatarPacketHandler() : base(ChatOnConnectAvatarPacket.ReadFrom)
        {
        }

        protected override void Handle(ChatOnConnectAvatarPacket packet)
        {
            throw new NotImplementedException($"ChatOnConnectAvatar (opcode 0x{Opcode:X8})");
        }
    }
}
