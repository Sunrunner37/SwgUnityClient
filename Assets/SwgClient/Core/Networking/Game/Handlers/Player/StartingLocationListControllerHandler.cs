using Swg.Client.Core.Networking.Game.Packets.Player;
using System;
using UnityEngine.Scripting;

namespace Swg.Client.Core.Networking.Game.Handlers.Player
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/StartingLocationListMessage.h:10 (StartingLocationListMessage).
    [Preserve]
    internal sealed class StartingLocationListControllerHandler : ObjectControllerHandler<StartingLocationListPacket>
    {
        public override uint ControllerType => StartingLocationListPacket.ControllerType;

        [Preserve]
        public StartingLocationListControllerHandler() : base(StartingLocationListPacket.ReadFrom)
        {
        }

        protected override void Handle(StartingLocationListPacket packet)
        {
            throw new NotImplementedException($"StartingLocationListMessage (controller 0x{ControllerType:X8})");
        }
    }
}
