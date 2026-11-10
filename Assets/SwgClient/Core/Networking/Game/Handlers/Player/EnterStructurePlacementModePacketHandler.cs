using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Player;

namespace Swg.Client.Core.Networking.Game.Handlers.Player
{
    // Core3/MMOCoreORB/src/server/zone/packets/player/EnterStructurePlacementModeMessage.h:10 (EnterStructurePlacementModeMessage).
    [Preserve]
    internal sealed class EnterStructurePlacementModePacketHandler : GamePacketHandler<EnterStructurePlacementModePacket>
    {
        public override uint Opcode => EnterStructurePlacementModePacket.Opcode;

        [Preserve]
        public EnterStructurePlacementModePacketHandler() : base(EnterStructurePlacementModePacket.ReadFrom)
        {
        }

        protected override void Handle(EnterStructurePlacementModePacket packet)
        {
            throw new NotImplementedException($"EnterStructurePlacementModeMessage (opcode 0x{Opcode:X8})");
        }
    }
}
