using Swg.Client.Runtime.Networking;
using UnityEngine.Scripting;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets;
using Swg.Client.Core.Networking.Game.Replication;

namespace Swg.Client.Core.Networking.Game.Handlers.Movement
{
    [Preserve]
    internal sealed class TransformWithParentControllerHandler : IObjectControllerHandler
    {
        readonly GameReplication replication;
        public uint ControllerType => 0xf1;

        [Preserve]
        public TransformWithParentControllerHandler(GameReplication replication) => this.replication = replication;
        public void Handle(ObjectControllerPacket packet)
        {
            var update = MovementReader.ReadControllerTransform(new ByteReader(packet.Payload), true);
            replication.Commit(packet.SwgId, update);
        }
    }
}
