using Swg.Client.Runtime.Networking;
using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Mission;

namespace Swg.Client.Core.Networking.Game.Handlers.Mission
{
    // Core3/MMOCoreORB/src/server/zone/packets/mission/PopulateMissionBrowserMessage.h:10 (PopulateMissionBrowserMessage).
    [Preserve]
    internal sealed class PopulateMissionBrowserPacketHandler : GamePacketHandler<PopulateMissionBrowserPacket>
    {
        readonly Lazy<ClientMissions> missions;
        readonly GameReplication replication;
        public override uint Opcode => PopulateMissionBrowserPacket.Opcode;

        [Preserve]
        public PopulateMissionBrowserPacketHandler(Lazy<ClientMissions> missions, GameReplication replication) : base(PopulateMissionBrowserPacket.ReadFrom)
        {
            this.missions = missions;
            this.replication = replication;
        }

        protected override void Handle(PopulateMissionBrowserPacket packet)
        {
            replication.UpdatePlayer(_ => missions.Value.Receive(packet));
        }
    }
}
