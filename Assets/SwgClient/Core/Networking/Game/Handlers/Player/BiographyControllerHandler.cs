using Swg.Client.Runtime.Networking;
using Swg.Client.Core.Characters;
using Swg.Client.Core.Networking.Game.Packets.Player;
using System;
using UnityEngine.Scripting;

namespace Swg.Client.Core.Networking.Game.Handlers.Player
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/Biography.h:11 (Biography).
    [Preserve]
    internal sealed class BiographyControllerHandler : ObjectControllerHandler<BiographyPacket>
    {
        public override uint ControllerType => BiographyPacket.ControllerType;

        readonly GameReplication replication;

        [Preserve]
        public BiographyControllerHandler(GameReplication replication) : base(BiographyPacket.ReadFrom)
        {
            this.replication = replication;
        }

        protected override void Handle(BiographyPacket packet)
        {
            replication.UpdatePlayer(player => { if (player.SwgId.Value == packet.PlayerSwgId) player.CharacterSheet.Update(data => data.Biography = packet.Biography, CharacterSheetChange.Personal); });
        }
    }
}
