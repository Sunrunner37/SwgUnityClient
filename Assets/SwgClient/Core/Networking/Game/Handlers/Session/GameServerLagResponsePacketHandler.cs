using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Session;

namespace Swg.Client.Core.Networking.Game.Handlers.Session
{
    // Core3/MMOCoreORB/src/server/zone/packets/zone/LagReport.h:14 (GameServerLagResponse).
    [Preserve]
    internal sealed class GameServerLagResponsePacketHandler : GamePacketHandler<GameServerLagResponsePacket>
    {
        public override uint Opcode => GameServerLagResponsePacket.Opcode;

        [Preserve]
        public GameServerLagResponsePacketHandler() : base(GameServerLagResponsePacket.ReadFrom)
        {
        }

        protected override void Handle(GameServerLagResponsePacket packet)
        {
            throw new NotImplementedException($"GameServerLagResponse (opcode 0x{Opcode:X8})");
        }
    }
}
