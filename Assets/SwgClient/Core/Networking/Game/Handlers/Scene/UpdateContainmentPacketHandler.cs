using Swg.Client.Core.Networking.Game.Packets.Scene;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets;
using Swg.Client.Core.Networking.Game.Replication;

namespace Swg.Client.Core.Networking.Game.Handlers.Scene
{
    [Preserve]
    internal sealed class UpdateContainmentPacketHandler : GamePacketHandler<UpdateContainmentPacket>
    {
        readonly GameWorldState world;
        public override uint Opcode => UpdateContainmentPacket.Opcode;

        [Preserve]
        public UpdateContainmentPacketHandler(GameWorldState world) : base(UpdateContainmentPacket.ReadFrom) => this.world = world;
        protected override void Handle(UpdateContainmentPacket packet)
        {
            world.Commit(packet.ObjectId, new GameObjectUpdate { ParentId = packet.ParentId, Arrangement = packet.Arrangement });
        }
    }
}
