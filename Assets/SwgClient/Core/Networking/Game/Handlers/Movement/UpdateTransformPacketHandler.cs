using Swg.Client.Core.Networking.Game.Packets.Movement;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets;
using Swg.Client.Core.Networking.Game.Replication;

namespace Swg.Client.Core.Networking.Game.Handlers.Movement
{
    [Preserve]
    internal sealed class UpdateTransformPacketHandler : GamePacketHandler<UpdateTransformPacket>
    {
        readonly GameWorldState world;
        public override uint Opcode => UpdateTransformPacket.Opcode;

        [Preserve]
        public UpdateTransformPacketHandler(GameWorldState world) : base(UpdateTransformPacket.ReadFrom) => this.world = world;
        protected override void Handle(UpdateTransformPacket packet)
        {
            world.Commit(packet.ObjectId, new GameObjectUpdate { ParentId = 0, Position = packet.Position, Rotation = MovementReader.Heading(packet.Direction), MovementCounter = packet.MovementCounter });
        }
    }
}
