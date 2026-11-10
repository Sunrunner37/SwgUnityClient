using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Harvester;

namespace Swg.Client.Core.Networking.Game.Handlers.Harvester
{
    // Core3/MMOCoreORB/src/server/zone/packets/harvester/ResourceHarvesterActivatePageMessage.h:11 (ResourceHarvesterActivatePageMessage).
    [Preserve]
    internal sealed class ResourceHarvesterActivatePagePacketHandler : GamePacketHandler<ResourceHarvesterActivatePagePacket>
    {
        public override uint Opcode => ResourceHarvesterActivatePagePacket.Opcode;

        [Preserve]
        public ResourceHarvesterActivatePagePacketHandler() : base(ResourceHarvesterActivatePagePacket.ReadFrom)
        {
        }

        protected override void Handle(ResourceHarvesterActivatePagePacket packet)
        {
            throw new NotImplementedException($"ResourceHarvesterActivatePageMessage (opcode 0x{Opcode:X8})");
        }
    }
}
