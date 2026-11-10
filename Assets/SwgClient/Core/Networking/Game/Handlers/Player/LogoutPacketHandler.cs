using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Player;

namespace Swg.Client.Core.Networking.Game.Handlers.Player
{
    // Core3/MMOCoreORB/src/server/zone/packets/player/LogoutMessage.h:11 (LogoutMessage).
    // Core3/MMOCoreORB/src/server/zone/packets/zone/ClientLogout.h:10 (ClientLogout).
    [Preserve]
    internal sealed class LogoutPacketHandler : GamePacketHandler<LogoutPacket>
    {
        public override uint Opcode => LogoutPacket.Opcode;

        [Preserve]
        public LogoutPacketHandler() : base(LogoutPacket.ReadFrom)
        {
        }

        protected override void Handle(LogoutPacket packet)
        {
            throw new NotImplementedException($"LogoutMessage (opcode 0x{Opcode:X8})");
        }
    }
}
