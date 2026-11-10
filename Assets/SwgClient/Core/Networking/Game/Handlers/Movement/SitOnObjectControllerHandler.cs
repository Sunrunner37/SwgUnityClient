using Swg.Client.Core.Networking.Game.Packets.Movement;
using System;
using UnityEngine.Scripting;

namespace Swg.Client.Core.Networking.Game.Handlers.Movement
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/SitOnObject.h:10 (SitOnObject).
    [Preserve]
    internal sealed class SitOnObjectControllerHandler : ObjectControllerHandler<SitOnObjectPacket>
    {
        public override uint ControllerType => SitOnObjectPacket.ControllerType;

        [Preserve]
        public SitOnObjectControllerHandler() : base(SitOnObjectPacket.ReadFrom)
        {
        }

        protected override void Handle(SitOnObjectPacket packet)
        {
            throw new NotImplementedException($"SitOnObject (controller 0x{ControllerType:X8})");
        }
    }
}
