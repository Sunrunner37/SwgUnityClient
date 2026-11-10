using Swg.Client.Core.Networking.Game.Packets.Harvester;
using System;
using UnityEngine.Scripting;

namespace Swg.Client.Core.Networking.Game.Handlers.Harvester
{
    // Core3/MMOCoreORB/src/server/zone/packets/harvester/HarvesterResourceDataMessage.h:10 (HarvesterResourceDataMessage).
    [Preserve]
    internal sealed class HarvesterResourceDataControllerHandler : ObjectControllerHandler<HarvesterResourceDataPacket>
    {
        public override uint ControllerType => HarvesterResourceDataPacket.ControllerType;

        [Preserve]
        public HarvesterResourceDataControllerHandler() : base(HarvesterResourceDataPacket.ReadFrom)
        {
        }

        protected override void Handle(HarvesterResourceDataPacket packet)
        {
            throw new NotImplementedException($"HarvesterResourceDataMessage (controller 0x{ControllerType:X8})");
        }
    }
}
