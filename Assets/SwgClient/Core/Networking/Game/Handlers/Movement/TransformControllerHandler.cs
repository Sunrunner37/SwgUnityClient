using Swg.Client.Runtime.Networking;
using UnityEngine.Scripting;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets;
using Swg.Client.Core.Networking.Game.Replication;

namespace Swg.Client.Core.Networking.Game.Handlers.Movement
{
    [Preserve]
    internal sealed class TransformControllerHandler : IObjectControllerHandler
    {
        readonly GameReplication replication;
        public uint ControllerType => 0x71;

        [Preserve]
        public TransformControllerHandler(GameReplication replication) => this.replication = replication;
        public void Handle(ObjectControllerPacket packet)
        {
            var update = MovementReader.ReadControllerTransform(new ByteReader(packet.Payload), false);
            replication.Commit(packet.SwgId, update);
        }
    }
}
