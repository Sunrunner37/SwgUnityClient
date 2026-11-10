using Swg.Client.Core.Networking.Game.Packets.Player;
using System;
using UnityEngine.Scripting;

namespace Swg.Client.Core.Networking.Game.Handlers.Player
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/Biography.h:11 (Biography).
    [Preserve]
    internal sealed class BiographyControllerHandler : ObjectControllerHandler<BiographyPacket>
    {
        public override uint ControllerType => BiographyPacket.ControllerType;

        [Preserve]
        public BiographyControllerHandler() : base(BiographyPacket.ReadFrom)
        {
        }

        protected override void Handle(BiographyPacket packet)
        {
            throw new NotImplementedException($"Biography (controller 0x{ControllerType:X8})");
        }
    }
}
