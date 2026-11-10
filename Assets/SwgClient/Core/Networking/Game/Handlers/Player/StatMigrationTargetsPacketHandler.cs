using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Player;

namespace Swg.Client.Core.Networking.Game.Handlers.Player
{
    // Core3/MMOCoreORB/src/server/zone/packets/player/StatMigrationTargetsMessage.h:10 (StatMigrationTargetsMessage).
    [Preserve]
    internal sealed class StatMigrationTargetsPacketHandler : GamePacketHandler<StatMigrationTargetsPacket>
    {
        public override uint Opcode => StatMigrationTargetsPacket.Opcode;

        [Preserve]
        public StatMigrationTargetsPacketHandler() : base(StatMigrationTargetsPacket.ReadFrom)
        {
        }

        protected override void Handle(StatMigrationTargetsPacket packet)
        {
            throw new NotImplementedException($"StatMigrationTargetsMessage (opcode 0x{Opcode:X8})");
        }
    }
}
