using Swg.Client.Runtime.Networking;
using Swg.Client.Core.Networking.Game.Packets.Scene;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets;
using Swg.Client.Core.Networking.Game.Replication;

namespace Swg.Client.Core.Networking.Game.Handlers.Scene
{
    [Preserve]
    internal sealed class SceneCreateObjectPacketHandler : GamePacketHandler<SceneCreateObjectPacket>
    {
        readonly GameReplication replication;
        public override uint Opcode => SceneCreateObjectPacket.Opcode;

        [Preserve]
        public SceneCreateObjectPacketHandler(GameReplication replication) : base(SceneCreateObjectPacket.ReadFrom) => this.replication = replication;
        protected override void Handle(SceneCreateObjectPacket packet)
        {
            replication.Commit(packet.ObjectId, new GameObjectUpdate { TemplateCrc = packet.TemplateCrc, Position = packet.Position, Rotation = packet.Rotation });
        }
    }
}
