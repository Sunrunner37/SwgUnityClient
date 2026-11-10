using Swg.Client.Core.Networking.Game.Packets.Player;
using System;
using UnityEngine.Scripting;

namespace Swg.Client.Core.Networking.Game.Handlers.Player
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/PlayersNearYou.h:13 (PlayersNearYouMessage).
    [Preserve]
    internal sealed class PlayersNearYouControllerHandler : ObjectControllerHandler<PlayersNearYouPacket>
    {
        public override uint ControllerType => PlayersNearYouPacket.ControllerType;

        [Preserve]
        public PlayersNearYouControllerHandler() : base(PlayersNearYouPacket.ReadFrom)
        {
        }

        protected override void Handle(PlayersNearYouPacket packet)
        {
            throw new NotImplementedException($"PlayersNearYouMessage (controller 0x{ControllerType:X8})");
        }
    }
}
