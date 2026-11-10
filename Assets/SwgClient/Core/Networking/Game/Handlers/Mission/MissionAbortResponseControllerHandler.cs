using Swg.Client.Core.Networking.Game.Packets.Mission;
using System;
using UnityEngine.Scripting;

namespace Swg.Client.Core.Networking.Game.Handlers.Mission
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/MissionAbortCallback.h:56 (MissionAbortResponse).
    [Preserve]
    internal sealed class MissionAbortResponseControllerHandler : ObjectControllerHandler<MissionAbortResponsePacket>
    {
        public override uint ControllerType => MissionAbortResponsePacket.ControllerType;

        [Preserve]
        public MissionAbortResponseControllerHandler() : base(MissionAbortResponsePacket.ReadFrom)
        {
        }

        protected override void Handle(MissionAbortResponsePacket packet)
        {
            throw new NotImplementedException($"MissionAbortResponse (controller 0x{ControllerType:X8})");
        }
    }
}
