using Swg.Client.Runtime.Networking;
using Swg.Client.Core.Networking.Game.Packets.Movement;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets;
using Swg.Client.Core.Networking.Game.Replication;

namespace Swg.Client.Core.Networking.Game.Handlers.Movement
{
    [Preserve]
    internal sealed class UpdateTransformWithParentPacketHandler : GamePacketHandler<UpdateTransformWithParentPacket>
    {
        readonly GameReplication replication;
        public override uint Opcode => UpdateTransformWithParentPacket.Opcode;

        [Preserve]
        public UpdateTransformWithParentPacketHandler(GameReplication replication) : base(UpdateTransformWithParentPacket.ReadFrom) => this.replication = replication;
        protected override void Handle(UpdateTransformWithParentPacket packet)
        {
            replication.Commit(packet.ObjectId, new GameObjectUpdate { ParentId = packet.ParentId, Position = packet.Position, Rotation = MovementReader.Heading(packet.Direction), MovementCounter = packet.MovementCounter });
        }
    }
}
