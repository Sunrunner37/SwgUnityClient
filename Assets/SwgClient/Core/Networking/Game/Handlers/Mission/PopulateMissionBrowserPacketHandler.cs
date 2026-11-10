using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Mission;

namespace Swg.Client.Core.Networking.Game.Handlers.Mission
{
    // Core3/MMOCoreORB/src/server/zone/packets/mission/PopulateMissionBrowserMessage.h:10 (PopulateMissionBrowserMessage).
    [Preserve]
    internal sealed class PopulateMissionBrowserPacketHandler : GamePacketHandler<PopulateMissionBrowserPacket>
    {
        public override uint Opcode => PopulateMissionBrowserPacket.Opcode;

        [Preserve]
        public PopulateMissionBrowserPacketHandler() : base(PopulateMissionBrowserPacket.ReadFrom)
        {
        }

        protected override void Handle(PopulateMissionBrowserPacket packet)
        {
            throw new NotImplementedException($"PopulateMissionBrowserMessage (opcode 0x{Opcode:X8})");
        }
    }
}
