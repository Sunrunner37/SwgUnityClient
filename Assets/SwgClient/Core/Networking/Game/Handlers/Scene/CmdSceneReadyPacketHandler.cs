using Swg.Client.Core.Networking.Game.Packets.Scene;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets;
using Swg.Client.Core.Networking.Game.Replication;

namespace Swg.Client.Core.Networking.Game.Handlers.Scene
{
    [Preserve]
    internal sealed class CmdSceneReadyPacketHandler : GamePacketHandler<CmdSceneReadyPacket>
    {
        readonly GameWorldState world;
        public override uint Opcode => CmdSceneReadyPacket.Opcode;

        [Preserve]
        public CmdSceneReadyPacketHandler(GameWorldState world) : base(CmdSceneReadyPacket.ReadFrom) => this.world = world;
        protected override void Handle(CmdSceneReadyPacket packet)
        {
            world.AcknowledgeSceneReady();
        }
    }
}
