using Swg.Client.Core.Networking.Game.Packets.Loot;
using System;
using UnityEngine.Scripting;

namespace Swg.Client.Core.Networking.Game.Handlers.Loot
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/OpenLotteryWindow.h:14 (OpenLotteryWindow).
    [Preserve]
    internal sealed class OpenLotteryWindowControllerHandler : ObjectControllerHandler<OpenLotteryWindowPacket>
    {
        public override uint ControllerType => OpenLotteryWindowPacket.ControllerType;

        [Preserve]
        public OpenLotteryWindowControllerHandler() : base(OpenLotteryWindowPacket.ReadFrom)
        {
        }

        protected override void Handle(OpenLotteryWindowPacket packet)
        {
            throw new NotImplementedException($"OpenLotteryWindow (controller 0x{ControllerType:X8})");
        }
    }
}
