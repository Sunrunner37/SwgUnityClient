using Swg.Client.Core.Networking.Game.Packets.Scene;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets;
using Swg.Client.Core.Networking.Game.Replication;

namespace Swg.Client.Core.Networking.Game.Handlers.Scene
{
    [Preserve]
    internal sealed class SceneEndBaselinesPacketHandler : GamePacketHandler<SceneEndBaselinesPacket>
    {
        readonly GameWorldState world;
        public override uint Opcode => SceneEndBaselinesPacket.Opcode;

        [Preserve]
        public SceneEndBaselinesPacketHandler(GameWorldState world) : base(SceneEndBaselinesPacket.ReadFrom) => this.world = world;
        protected override void Handle(SceneEndBaselinesPacket packet)
        {
            world.Commit(packet.ObjectId, new GameObjectUpdate { BaselinesComplete = true });
        }
    }
}
