using UnityEngine.Scripting;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets;
using Swg.Client.Core.Networking.Game.Replication;

namespace Swg.Client.Core.Networking.Game.Handlers.Movement
{
    [Preserve]
    internal sealed class TransformControllerHandler : IObjectControllerHandler
    {
        readonly GameWorldState world;
        public uint ControllerType => 0x71;

        [Preserve]
        public TransformControllerHandler(GameWorldState world) => this.world = world;
        public void Handle(ObjectControllerPacket packet)
        {
            var update = MovementReader.ReadControllerTransform(new ByteReader(packet.Payload), false);
            world.Commit(packet.ObjectId, update);
        }
    }
}
