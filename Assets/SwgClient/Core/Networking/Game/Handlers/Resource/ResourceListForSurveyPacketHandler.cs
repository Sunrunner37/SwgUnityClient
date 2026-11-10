using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Resource;

namespace Swg.Client.Core.Networking.Game.Handlers.Resource
{
    // Core3/MMOCoreORB/src/server/zone/packets/resource/ResourceListForSurveyMessage.h:10 (ResourceListForSurveyMessage).
    [Preserve]
    internal sealed class ResourceListForSurveyPacketHandler : GamePacketHandler<ResourceListForSurveyPacket>
    {
        public override uint Opcode => ResourceListForSurveyPacket.Opcode;

        [Preserve]
        public ResourceListForSurveyPacketHandler() : base(ResourceListForSurveyPacket.ReadFrom)
        {
        }

        protected override void Handle(ResourceListForSurveyPacket packet)
        {
            throw new NotImplementedException($"ResourceListForSurveyMessage (opcode 0x{Opcode:X8})");
        }
    }
}
