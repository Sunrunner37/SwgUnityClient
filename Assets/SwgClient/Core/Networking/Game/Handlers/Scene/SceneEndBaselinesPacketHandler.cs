using Swg.Client.Runtime.Networking;
using Swg.Client.Core.Networking.Game.Packets.Scene;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets;
using Swg.Client.Core.Networking.Game.Replication;

namespace Swg.Client.Core.Networking.Game.Handlers.Scene
{
    [Preserve]
    internal sealed class SceneEndBaselinesPacketHandler : GamePacketHandler<SceneEndBaselinesPacket>
    {
        readonly GameReplication replication;
        public override uint Opcode => SceneEndBaselinesPacket.Opcode;

        [Preserve]
        public SceneEndBaselinesPacketHandler(GameReplication replication) : base(SceneEndBaselinesPacket.ReadFrom) => this.replication = replication;
        protected override void Handle(SceneEndBaselinesPacket packet)
        {
            replication.Commit(packet.SwgId, new GameObjectUpdate { BaselinesComplete = true });
        }
    }
}
