using Swg.Client.Runtime.Networking;
using Swg.Client.Core.Networking.Game.Replication;
using Swg.Client.Core.Characters;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Scene;

namespace Swg.Client.Core.Networking.Game.Handlers.Scene
{
    // Core3/MMOCoreORB/src/server/zone/packets/cell/UpdateCellPermissionsMessage.h:10 (UpdateCellPermissionsMessage).
    [Preserve]
    internal sealed class UpdateCellPermissionsPacketHandler : GamePacketHandler<UpdateCellPermissionsPacket>
    {
        readonly GameReplication replication;
        public override uint Opcode => UpdateCellPermissionsPacket.Opcode;

        [Preserve]
        public UpdateCellPermissionsPacketHandler(GameReplication replication) : base(UpdateCellPermissionsPacket.ReadFrom)
        {
            this.replication = replication;
        }

        protected override void Handle(UpdateCellPermissionsPacket packet)
        {
            replication.Commit(packet.CellSwgId, new CellSceneObjectUpdate { AllowEntry = packet.AllowEntry != 0 });
        }
    }
}
