using Swg.Client.Core.Networking.Game.Packets.Tutorial;
using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Player;

namespace Swg.Client.Core.Networking.Game.Handlers.Tutorial
{
    // Core3/MMOCoreORB/src/server/zone/packets/player/NewbieTutorialRequest.h:31 (NewbieTutorialRequest).
    [Preserve]
    internal sealed class NewbieTutorialRequestPacketHandler : GamePacketHandler<NewbieTutorialRequestPacket>
    {
        public override uint Opcode => NewbieTutorialRequestPacket.Opcode;

        [Preserve]
        public NewbieTutorialRequestPacketHandler() : base(NewbieTutorialRequestPacket.ReadFrom)
        {
        }

        protected override void Handle(NewbieTutorialRequestPacket packet)
        {
            throw new NotImplementedException($"NewbieTutorialRequest (opcode 0x{Opcode:X8})");
        }
    }
}
