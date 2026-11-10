using Swg.Client.Core.Networking.Game.Packets.Effects;
using System;
using UnityEngine.Scripting;

namespace Swg.Client.Core.Networking.Game.Handlers.Effects
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/Flourish.h:10 (Flourish).
    [Preserve]
    internal sealed class FlourishControllerHandler : ObjectControllerHandler<FlourishPacket>
    {
        public override uint ControllerType => FlourishPacket.ControllerType;

        [Preserve]
        public FlourishControllerHandler() : base(FlourishPacket.ReadFrom)
        {
        }

        protected override void Handle(FlourishPacket packet)
        {
            throw new NotImplementedException($"Flourish (controller 0x{ControllerType:X8})");
        }
    }
}
