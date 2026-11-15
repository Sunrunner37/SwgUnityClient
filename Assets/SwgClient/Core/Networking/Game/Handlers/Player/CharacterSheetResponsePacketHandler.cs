using Swg.Client.Runtime.Networking;
using Swg.Client.Core.Characters;
using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Player;

namespace Swg.Client.Core.Networking.Game.Handlers.Player
{
    // Core3/MMOCoreORB/src/server/zone/packets/player/CharacterSheetResponseMessage.h:11 (CharacterSheetResponseMessage).
    [Preserve]
    internal sealed class CharacterSheetResponsePacketHandler : GamePacketHandler<CharacterSheetResponsePacket>
    {
        public override uint Opcode => CharacterSheetResponsePacket.Opcode;

        readonly GameReplication replication;

        [Preserve]
        public CharacterSheetResponsePacketHandler(GameReplication replication) : base(CharacterSheetResponsePacket.ReadFrom)
        {
            this.replication = replication;
        }

        protected override void Handle(CharacterSheetResponsePacket packet)
        {
            replication.UpdatePlayer(player => player.CharacterSheet.Update(data =>
            {
                data.Spouse = packet.SpouseName;
                data.ResidencePlanet = packet.ResidencePlanet;
                data.ResidencePosition = packet.ResidencePosition;
                data.CloningPlanet = packet.CloningPlanet;
                data.CloningPosition = packet.CloningPosition;
                data.BankPlanet = packet.BankPlanet;
                data.LotsRemaining = packet.LotsRemaining;
                data.Faction = packet.Faction;
                data.FactionStatus = packet.FactionStatus;
            }, CharacterSheetChange.Status | CharacterSheetChange.Personal));
        }
    }
}
