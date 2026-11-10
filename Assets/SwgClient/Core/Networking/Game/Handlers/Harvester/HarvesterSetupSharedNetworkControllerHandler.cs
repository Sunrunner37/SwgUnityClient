using Swg.Client.Core.Networking.Game.Packets.Harvester;
using System;
using UnityEngine.Scripting;

namespace Swg.Client.Core.Networking.Game.Handlers.Harvester
{
    // Core3/MMOCoreORB/src/server/zone/packets/installation/HarvesterSetupSharedNetworkMessage.h:14 (HarvesterSetupSharedNetworkMessage).
    [Preserve]
    internal sealed class HarvesterSetupSharedNetworkControllerHandler : ObjectControllerHandler<HarvesterSetupSharedNetworkPacket>
    {
        public override uint ControllerType => HarvesterSetupSharedNetworkPacket.ControllerType;

        [Preserve]
        public HarvesterSetupSharedNetworkControllerHandler() : base(HarvesterSetupSharedNetworkPacket.ReadFrom)
        {
        }

        protected override void Handle(HarvesterSetupSharedNetworkPacket packet)
        {
            throw new NotImplementedException($"HarvesterSetupSharedNetworkMessage (controller 0x{ControllerType:X8})");
        }
    }
}
