using Swg.Client.Runtime.Networking;
using Swg.Client.Core.Networking.Game.Packets.Scene;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets;
using Swg.Client.Core.Networking.Game.Replication;

namespace Swg.Client.Core.Networking.Game.Handlers.Scene
{
    [Preserve]
    internal sealed class SceneDestroyObjectPacketHandler : GamePacketHandler<SceneDestroyObjectPacket>
    {
        readonly GameReplication replication;
        public override uint Opcode => SceneDestroyObjectPacket.Opcode;

        [Preserve]
        public SceneDestroyObjectPacketHandler(GameReplication replication) : base(SceneDestroyObjectPacket.ReadFrom) => this.replication = replication;
        protected override void Handle(SceneDestroyObjectPacket packet)
        {
            replication.Remove(packet.SwgId);
        }
    }
}
