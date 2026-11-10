using Swg.Client.Runtime.Networking;
using Swg.Client.Core.Networking.Game.Packets.Scene;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets;
using Swg.Client.Core.Networking.Game.Replication;

namespace Swg.Client.Core.Networking.Game.Handlers.Scene
{
    [Preserve]
    internal sealed class CmdSceneReadyPacketHandler : GamePacketHandler<CmdSceneReadyPacket>
    {
        readonly GameReplication replication;
        public override uint Opcode => CmdSceneReadyPacket.Opcode;

        [Preserve]
        public CmdSceneReadyPacketHandler(GameReplication replication) : base(CmdSceneReadyPacket.ReadFrom) => this.replication = replication;
        protected override void Handle(CmdSceneReadyPacket packet)
        {
            replication.AcknowledgeSceneReady();
        }
    }
}
