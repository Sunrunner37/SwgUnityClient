using Swg.Client.Runtime.Networking;
using Swg.Client.Core.Networking.Game.Replication;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Combat;

namespace Swg.Client.Core.Networking.Game.Handlers.Combat
{
    // Core3/MMOCoreORB/src/server/zone/packets/tangible/UpdatePVPStatusMessage.h:11 (UpdatePVPStatusMessage).
    [Preserve]
    internal sealed class UpdatePVPStatusPacketHandler : GamePacketHandler<UpdatePVPStatusPacket>
    {
        readonly GameReplication replication;
        public override uint Opcode => UpdatePVPStatusPacket.Opcode;

        [Preserve]
        public UpdatePVPStatusPacketHandler(GameReplication replication) : base(UpdatePVPStatusPacket.ReadFrom)
        {
            this.replication = replication;
        }

        protected override void Handle(UpdatePVPStatusPacket packet)
        {
            replication.Commit(packet.SwgId, new TangibleSceneObjectUpdate { PvpStatus = packet.PvpStatus });
        }
    }
}
