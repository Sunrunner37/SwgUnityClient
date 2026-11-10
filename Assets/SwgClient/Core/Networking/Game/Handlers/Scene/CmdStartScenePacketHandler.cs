using Swg.Client.Core.Networking.Game.Packets.Scene;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets;
using Swg.Client.Core.Networking.Game.Replication;

namespace Swg.Client.Core.Networking.Game.Handlers.Scene
{
    [Preserve]
    internal sealed class CmdStartScenePacketHandler : GamePacketHandler<CmdStartScenePacket>
    {
        readonly GameWorldState world;
        public override uint Opcode => CmdStartScenePacket.Opcode;

        [Preserve]
        public CmdStartScenePacketHandler(GameWorldState world) : base(CmdStartScenePacket.ReadFrom) => this.world = world;
        protected override void Handle(CmdStartScenePacket packet)
        {
            world.StartScene(packet);
        }
    }
}
