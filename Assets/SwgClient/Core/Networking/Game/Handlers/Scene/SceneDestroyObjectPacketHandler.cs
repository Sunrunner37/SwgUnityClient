using Swg.Client.Core.Networking.Game.Packets.Scene;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets;
using Swg.Client.Core.Networking.Game.Replication;

namespace Swg.Client.Core.Networking.Game.Handlers.Scene
{
    [Preserve]
    internal sealed class SceneDestroyObjectPacketHandler : GamePacketHandler<SceneDestroyObjectPacket>
    {
        readonly GameWorldState world;
        public override uint Opcode => SceneDestroyObjectPacket.Opcode;

        [Preserve]
        public SceneDestroyObjectPacketHandler(GameWorldState world) : base(SceneDestroyObjectPacket.ReadFrom) => this.world = world;
        protected override void Handle(SceneDestroyObjectPacket packet)
        {
            world.Remove(packet.ObjectId);
        }
    }
}
