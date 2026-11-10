using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Ui;

namespace Swg.Client.Core.Networking.Game.Handlers.Ui
{
    // Core3/MMOCoreORB/src/server/zone/packets/ui/LaunchBrowserMessage.h:13 (LaunchBrowserMessage).
    [Preserve]
    internal sealed class LaunchBrowserPacketHandler : GamePacketHandler<LaunchBrowserPacket>
    {
        public override uint Opcode => LaunchBrowserPacket.Opcode;

        [Preserve]
        public LaunchBrowserPacketHandler() : base(LaunchBrowserPacket.ReadFrom)
        {
        }

        protected override void Handle(LaunchBrowserPacket packet)
        {
            throw new NotImplementedException($"LaunchBrowserMessage (opcode 0x{Opcode:X8})");
        }
    }
}
