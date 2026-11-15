using Swg.Client.Runtime.Networking;
using Swg.Client.Core.Characters;
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

        readonly GameReplication replication;

        [Preserve]
        public StatMigrationTargetsPacketHandler(GameReplication replication) : base(StatMigrationTargetsPacket.ReadFrom)
        {
            this.replication = replication;
        }

        protected override void Handle(StatMigrationTargetsPacket packet)
        {
            replication.UpdatePlayer(player => player.CharacterSheet.Update(data => { data.MigrationTargets = HamAttributes.FromArray(packet.Attributes); data.MigrationBudget = data.MigrationTargets.Total + packet.RemainingPoints; data.HasMigrationTargets = true; }, CharacterSheetChange.Migration));
        }
    }
}
