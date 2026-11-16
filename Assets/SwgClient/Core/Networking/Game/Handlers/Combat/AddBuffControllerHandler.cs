using Swg.Client.Core.Networking.Game.Packets.Combat;
using Swg.Client.Runtime.Networking;
using UnityEngine;
using UnityEngine.Scripting;

namespace Swg.Client.Core.Networking.Game.Handlers.Combat
{
    [Preserve]
    internal sealed class AddBuffControllerHandler : ObjectControllerHandler<AddBuffPacket>
    {
        readonly GameReplication replication;
        public override uint ControllerType => AddBuffPacket.ControllerType;
        [Preserve]
        public AddBuffControllerHandler(GameReplication replication) : base(AddBuffPacket.ReadFrom) => this.replication = replication;
        protected override void Handle(AddBuffPacket packet) => replication.UpdatePlayer(player =>
        {
            if (player.SwgId.Value == packet.ReceiverSwgId)
                player.AddBuff(packet.BuffCrc, Time.realtimeSinceStartupAsDouble + packet.Duration);
        });
    }
}
