using Swg.Client.Runtime.Networking;
using Swg.Client.Core.Networking.Game.Packets.Mission;
using System;
using UnityEngine.Scripting;

namespace Swg.Client.Core.Networking.Game.Handlers.Mission
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/MissionAbortCallback.h:56 (MissionAbortResponse).
    [Preserve]
    internal sealed class MissionAbortResponseControllerHandler : ObjectControllerHandler<MissionAbortResponsePacket>
    {
        readonly Lazy<ClientMissions> missions;
        readonly GameReplication replication;
        public override uint ControllerType => MissionAbortResponsePacket.ControllerType;

        [Preserve]
        public MissionAbortResponseControllerHandler(Lazy<ClientMissions> missions, GameReplication replication) : base(MissionAbortResponsePacket.ReadFrom)
        {
            this.missions = missions;
            this.replication = replication;
        }

        protected override void Handle(MissionAbortResponsePacket packet)
        {
            replication.UpdatePlayer(_ => missions.Value.Receive(packet));
        }
    }
}
