using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Chat;

namespace Swg.Client.Core.Networking.Game.Handlers.Chat
{
    // Core3/MMOCoreORB/src/server/zone/packets/chat/ChatOnReceiveRoomInvitation.h:10 (ChatOnReceiveRoomInvitation).
    [Preserve]
    internal sealed class ChatOnReceiveRoomInvitationPacketHandler : GamePacketHandler<ChatOnReceiveRoomInvitationPacket>
    {
        public override uint Opcode => ChatOnReceiveRoomInvitationPacket.Opcode;

        [Preserve]
        public ChatOnReceiveRoomInvitationPacketHandler() : base(ChatOnReceiveRoomInvitationPacket.ReadFrom)
        {
        }

        protected override void Handle(ChatOnReceiveRoomInvitationPacket packet)
        {
            throw new NotImplementedException($"ChatOnReceiveRoomInvitation (opcode 0x{Opcode:X8})");
        }
    }
}
