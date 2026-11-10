using Swg.Client.Runtime.Networking;
using Swg.Client.Core.Networking.Game.Packets.Scene;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets;
using Swg.Client.Core.Networking.Game.Replication;

namespace Swg.Client.Core.Networking.Game.Handlers.Scene
{
    [Preserve]
    internal sealed class CmdStartScenePacketHandler : GamePacketHandler<CmdStartScenePacket>
    {
        readonly GameReplication replication;
        public override uint Opcode => CmdStartScenePacket.Opcode;

        [Preserve]
        public CmdStartScenePacketHandler(GameReplication replication) : base(CmdStartScenePacket.ReadFrom) => this.replication = replication;
        protected override void Handle(CmdStartScenePacket packet)
        {
            replication.StartScene(packet);
        }
    }
}
