using Swg.Client.Core.Networking.Game.Packets.Scene;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets;
using Swg.Client.Core.Networking.Game.Replication;

namespace Swg.Client.Core.Networking.Game.Handlers.Scene
{
    [Preserve]
    internal sealed class SceneCreateObjectPacketHandler : GamePacketHandler<SceneCreateObjectPacket>
    {
        readonly GameWorldState world;
        public override uint Opcode => SceneCreateObjectPacket.Opcode;

        [Preserve]
        public SceneCreateObjectPacketHandler(GameWorldState world) : base(SceneCreateObjectPacket.ReadFrom) => this.world = world;
        protected override void Handle(SceneCreateObjectPacket packet)
        {
            world.Commit(packet.ObjectId, new GameObjectUpdate { TemplateCrc = packet.TemplateCrc, Position = packet.Position, Rotation = packet.Rotation });
        }
    }
}
