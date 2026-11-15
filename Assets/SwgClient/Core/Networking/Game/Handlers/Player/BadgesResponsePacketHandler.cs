using Swg.Client.Runtime.Networking;
using Swg.Client.Core.Characters;
using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Player;

namespace Swg.Client.Core.Networking.Game.Handlers.Player
{
    // Core3/MMOCoreORB/src/server/zone/packets/player/BadgesResponseMessage.h:12 (BadgesResponseMessage).
    [Preserve]
    internal sealed class BadgesResponsePacketHandler : GamePacketHandler<BadgesResponsePacket>
    {
        public override uint Opcode => BadgesResponsePacket.Opcode;

        readonly GameReplication replication;

        [Preserve]
        public BadgesResponsePacketHandler(GameReplication replication) : base(BadgesResponsePacket.ReadFrom)
        {
            this.replication = replication;
        }

        protected override void Handle(BadgesResponsePacket packet)
        {
            replication.UpdatePlayer(player => { if (player.SwgId.Value == packet.PlayerSwgId) player.CharacterSheet.Update(data => data.BadgeMasks = packet.Bitmasks, CharacterSheetChange.Badges); });
        }
    }
}
