using Swg.Client.Core.Networking.Game.Packets.Chat;
using System;
using UnityEngine.Scripting;

namespace Swg.Client.Core.Networking.Game.Handlers.Chat
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/Emote.h:10 (Emote).
    [Preserve]
    internal sealed class EmoteControllerHandler : ObjectControllerHandler<EmotePacket>
    {
        public override uint ControllerType => EmotePacket.ControllerType;

        [Preserve]
        public EmoteControllerHandler() : base(EmotePacket.ReadFrom)
        {
        }

        protected override void Handle(EmotePacket packet)
        {
            throw new NotImplementedException($"Emote (controller 0x{ControllerType:X8})");
        }
    }
}
