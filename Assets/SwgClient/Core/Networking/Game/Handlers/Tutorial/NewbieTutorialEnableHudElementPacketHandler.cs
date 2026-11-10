using Swg.Client.Core.Networking.Game.Packets.Tutorial;
using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Ui;

namespace Swg.Client.Core.Networking.Game.Handlers.Tutorial
{
    // Core3/MMOCoreORB/src/server/zone/packets/ui/NewbieTutorialEnableHudElement.h:19 (NewbieTutorialEnableHudElement).
    [Preserve]
    internal sealed class NewbieTutorialEnableHudElementPacketHandler : GamePacketHandler<NewbieTutorialEnableHudElementPacket>
    {
        public override uint Opcode => NewbieTutorialEnableHudElementPacket.Opcode;

        [Preserve]
        public NewbieTutorialEnableHudElementPacketHandler() : base(NewbieTutorialEnableHudElementPacket.ReadFrom)
        {
        }

        protected override void Handle(NewbieTutorialEnableHudElementPacket packet)
        {
            throw new NotImplementedException($"NewbieTutorialEnableHudElement (opcode 0x{Opcode:X8})");
        }
    }
}
