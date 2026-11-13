using Swg.Client.Runtime.Networking;
using Swg.Client.Core.Networking.Game.Packets.Scene;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets;
using Swg.Client.Core.Networking.Game.Replication;

namespace Swg.Client.Core.Networking.Game.Handlers.Scene
{
    [Preserve]
    internal sealed class UpdateContainmentPacketHandler : GamePacketHandler<UpdateContainmentPacket>
    {
        readonly GameReplication replication;
        public override uint Opcode => UpdateContainmentPacket.Opcode;

        [Preserve]
        public UpdateContainmentPacketHandler(GameReplication replication) : base(UpdateContainmentPacket.ReadFrom) => this.replication = replication;
        protected override void Handle(UpdateContainmentPacket packet)
        {
            replication.Commit(packet.SwgId, new ContainmentUpdate(packet.ParentSwgId, packet.Arrangement));
        }
    }
}
