using Swg.Client.Core.Networking.Game.Packets.Combat;
using System;
using UnityEngine.Scripting;

namespace Swg.Client.Core.Networking.Game.Handlers.Combat
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/CombatAction.h:11 (CombatAction).
    [Preserve]
    internal sealed class CombatActionControllerHandler : ObjectControllerHandler<CombatActionPacket>
    {
        public override uint ControllerType => CombatActionPacket.ControllerType;

        [Preserve]
        public CombatActionControllerHandler() : base(CombatActionPacket.ReadFrom)
        {
        }

        protected override void Handle(CombatActionPacket packet)
        {
            throw new NotImplementedException($"CombatAction (controller 0x{ControllerType:X8})");
        }
    }
}
