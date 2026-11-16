using Swg.Client.Core.Networking.Game.Packets.Combat;
using Swg.Client.Runtime.Networking;
using UnityEngine;
using UnityEngine.Scripting;

namespace Swg.Client.Core.Networking.Game.Handlers.Combat
{
    [Preserve]
    internal sealed class RemoveBuffControllerHandler : ObjectControllerHandler<RemoveBuffPacket>
    {
        readonly GameReplication replication;
        public override uint ControllerType => RemoveBuffPacket.ControllerType;
        [Preserve]
        public RemoveBuffControllerHandler(GameReplication replication) : base(RemoveBuffPacket.ReadFrom) => this.replication = replication;
        protected override void Handle(RemoveBuffPacket packet) => replication.UpdatePlayer(player =>
        {
            if (player.SwgId.Value == packet.ReceiverSwgId)
                player.RemoveBuff(packet.BuffCrc);
        });
    }
}
