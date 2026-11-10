using Swg.Client.Core.Networking.Game.Packets.Effects;
using System;
using UnityEngine.Scripting;

namespace Swg.Client.Core.Networking.Game.Handlers.Effects
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/ShowFlyText.h:11 (ShowFlyText).
    [Preserve]
    internal sealed class ShowFlyTextControllerHandler : ObjectControllerHandler<ShowFlyTextPacket>
    {
        public override uint ControllerType => ShowFlyTextPacket.ControllerType;

        [Preserve]
        public ShowFlyTextControllerHandler() : base(ShowFlyTextPacket.ReadFrom)
        {
        }

        protected override void Handle(ShowFlyTextPacket packet)
        {
            throw new NotImplementedException($"ShowFlyText (controller 0x{ControllerType:X8})");
        }
    }
}
