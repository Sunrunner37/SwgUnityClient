using Swg.Client.Runtime.Networking;
using Swg.Client.Core.Networking.Game.Packets.Mission;
using System;
using UnityEngine.Scripting;

namespace Swg.Client.Core.Networking.Game.Handlers.Mission
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/MissionAcceptCallback.h:80 (MissionAcceptResponse).
    [Preserve]
    internal sealed class MissionAcceptResponseControllerHandler : ObjectControllerHandler<MissionAcceptResponsePacket>
    {
        readonly Lazy<ClientMissions> missions;
        readonly GameReplication replication;
        public override uint ControllerType => MissionAcceptResponsePacket.ControllerType;

        [Preserve]
        public MissionAcceptResponseControllerHandler(Lazy<ClientMissions> missions, GameReplication replication) : base(MissionAcceptResponsePacket.ReadFrom)
        {
            this.missions = missions;
            this.replication = replication;
        }

        protected override void Handle(MissionAcceptResponsePacket packet)
        {
            replication.UpdatePlayer(_ => missions.Value.Receive(packet));
        }
    }
}
