using Swg.Client.Runtime.Networking;
using Swg.Client.Core.Networking.Game.Packets.Movement;
using Swg.Client.Core.Networking.Game.Replication;
using UnityEngine.Scripting;

namespace Swg.Client.Core.Networking.Game.Handlers.Movement
{
    [Preserve]
    internal sealed class PostureControllerHandler : ObjectControllerHandler<PosturePacket>
    {
        readonly GameReplication replication;
        public override uint ControllerType => PosturePacket.ControllerType;
        [Preserve]
        public PostureControllerHandler(GameReplication replication) : base(PosturePacket.ReadFrom) => this.replication = replication;
        protected override void Handle(PosturePacket packet) => replication.Commit(packet.ReceiverSwgId, new GameObjectUpdate { Posture = packet.Posture });
    }
}
