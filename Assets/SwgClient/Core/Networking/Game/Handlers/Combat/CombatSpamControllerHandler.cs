using Swg.Client.Core.Networking.Game.Packets.Combat;
using System;
using UnityEngine.Scripting;

namespace Swg.Client.Core.Networking.Game.Handlers.Combat
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/CombatSpam.h:12 (CombatSpam).
    [Preserve]
    internal sealed class CombatSpamControllerHandler : ObjectControllerHandler<CombatSpamPacket>
    {
        public override uint ControllerType => CombatSpamPacket.ControllerType;

        [Preserve]
        public CombatSpamControllerHandler() : base(CombatSpamPacket.ReadFrom)
        {
        }

        protected override void Handle(CombatSpamPacket packet)
        {
            throw new NotImplementedException($"CombatSpam (controller 0x{ControllerType:X8})");
        }
    }
}
