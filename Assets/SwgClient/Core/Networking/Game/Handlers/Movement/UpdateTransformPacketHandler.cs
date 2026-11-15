using Swg.Client.Runtime.Networking;
using Swg.Client.Core.Networking.Game.Packets.Movement;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets;
using Swg.Client.Core.Networking.Game.Replication;

namespace Swg.Client.Core.Networking.Game.Handlers.Movement
{
    [Preserve]
    internal sealed class UpdateTransformPacketHandler : GamePacketHandler<UpdateTransformPacket>
    {
        readonly GameReplication replication;
        public override uint Opcode => UpdateTransformPacket.Opcode;

        [Preserve]
        public UpdateTransformPacketHandler(GameReplication replication) : base(UpdateTransformPacket.ReadFrom) => this.replication = replication;
        protected override void Handle(UpdateTransformPacket packet)
        {
            replication.Commit(packet.SwgId, new MovementUpdate(packet.Position, MovementReader.Heading(packet.Direction), 0, packet.MovementCounter, packet.Speed), isBroadcast: true);
        }
    }
}
