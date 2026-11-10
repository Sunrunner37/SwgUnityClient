using Swg.Client.Core.Networking.Game.Packets.Movement;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets;
using Swg.Client.Core.Networking.Game.Replication;

namespace Swg.Client.Core.Networking.Game.Handlers.Movement
{
    [Preserve]
    internal sealed class UpdateTransformWithParentPacketHandler : GamePacketHandler<UpdateTransformWithParentPacket>
    {
        readonly GameWorldState world;
        public override uint Opcode => UpdateTransformWithParentPacket.Opcode;

        [Preserve]
        public UpdateTransformWithParentPacketHandler(GameWorldState world) : base(UpdateTransformWithParentPacket.ReadFrom) => this.world = world;
        protected override void Handle(UpdateTransformWithParentPacket packet)
        {
            world.Commit(packet.ObjectId, new GameObjectUpdate { ParentId = packet.ParentId, Position = packet.Position, Rotation = MovementReader.Heading(packet.Direction), MovementCounter = packet.MovementCounter });
        }
    }
}
