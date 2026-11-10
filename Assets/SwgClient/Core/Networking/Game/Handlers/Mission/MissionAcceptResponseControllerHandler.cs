using Swg.Client.Core.Networking.Game.Packets.Mission;
using System;
using UnityEngine.Scripting;

namespace Swg.Client.Core.Networking.Game.Handlers.Mission
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/MissionAcceptCallback.h:80 (MissionAcceptResponse).
    [Preserve]
    internal sealed class MissionAcceptResponseControllerHandler : ObjectControllerHandler<MissionAcceptResponsePacket>
    {
        public override uint ControllerType => MissionAcceptResponsePacket.ControllerType;

        [Preserve]
        public MissionAcceptResponseControllerHandler() : base(MissionAcceptResponsePacket.ReadFrom)
        {
        }

        protected override void Handle(MissionAcceptResponsePacket packet)
        {
            throw new NotImplementedException($"MissionAcceptResponse (controller 0x{ControllerType:X8})");
        }
    }
}
