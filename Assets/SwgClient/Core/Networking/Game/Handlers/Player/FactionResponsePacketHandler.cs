using Swg.Client.Runtime.Networking;
using Swg.Client.Core.Characters;
using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Player;

namespace Swg.Client.Core.Networking.Game.Handlers.Player
{
    // Core3/MMOCoreORB/src/server/zone/packets/player/FactionResponseMessage.h:10 (FactionResponseMessage).
    [Preserve]
    internal sealed class FactionResponsePacketHandler : GamePacketHandler<FactionResponsePacket>
    {
        public override uint Opcode => FactionResponsePacket.Opcode;

        readonly GameReplication replication;

        [Preserve]
        public FactionResponsePacketHandler(GameReplication replication) : base(FactionResponsePacket.ReadFrom)
        {
            this.replication = replication;
        }

        protected override void Handle(FactionResponsePacket packet)
        {
            replication.UpdatePlayer(player => player.CharacterSheet.Update(data => { data.ImperialPoints = packet.ImperialPoints; data.RebelPoints = packet.RebelPoints; }, CharacterSheetChange.Status));
        }
    }
}
