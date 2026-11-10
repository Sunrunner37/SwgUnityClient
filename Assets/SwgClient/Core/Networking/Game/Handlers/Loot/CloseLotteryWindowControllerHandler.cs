using Swg.Client.Core.Networking.Game.Packets.Loot;
using System;
using UnityEngine.Scripting;

namespace Swg.Client.Core.Networking.Game.Handlers.Loot
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/CloseLotteryWindow.h:14 (CloseLotteryWindow).
    [Preserve]
    internal sealed class CloseLotteryWindowControllerHandler : ObjectControllerHandler<CloseLotteryWindowPacket>
    {
        public override uint ControllerType => CloseLotteryWindowPacket.ControllerType;

        [Preserve]
        public CloseLotteryWindowControllerHandler() : base(CloseLotteryWindowPacket.ReadFrom)
        {
        }

        protected override void Handle(CloseLotteryWindowPacket packet)
        {
            throw new NotImplementedException($"CloseLotteryWindow (controller 0x{ControllerType:X8})");
        }
    }
}
