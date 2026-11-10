using Swg.Client.Core.Networking.Game.Packets.Combat;
using System;
using UnityEngine.Scripting;

namespace Swg.Client.Core.Networking.Game.Handlers.Combat
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/WeaponRanges.h:14 (WeaponRanges).
    [Preserve]
    internal sealed class WeaponRangesControllerHandler : ObjectControllerHandler<WeaponRangesPacket>
    {
        public override uint ControllerType => WeaponRangesPacket.ControllerType;

        [Preserve]
        public WeaponRangesControllerHandler() : base(WeaponRangesPacket.ReadFrom)
        {
        }

        protected override void Handle(WeaponRangesPacket packet)
        {
            throw new NotImplementedException($"WeaponRanges (controller 0x{ControllerType:X8})");
        }
    }
}
