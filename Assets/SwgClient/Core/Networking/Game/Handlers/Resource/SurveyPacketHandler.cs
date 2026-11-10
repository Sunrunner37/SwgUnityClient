using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Resource;

namespace Swg.Client.Core.Networking.Game.Handlers.Resource
{
    // Core3/MMOCoreORB/src/server/zone/packets/resource/SurveyMessage.h:10 (SurveyMessage).
    [Preserve]
    internal sealed class SurveyPacketHandler : GamePacketHandler<SurveyPacket>
    {
        public override uint Opcode => SurveyPacket.Opcode;

        [Preserve]
        public SurveyPacketHandler() : base(SurveyPacket.ReadFrom)
        {
        }

        protected override void Handle(SurveyPacket packet)
        {
            throw new NotImplementedException($"SurveyMessage (opcode 0x{Opcode:X8})");
        }
    }
}
