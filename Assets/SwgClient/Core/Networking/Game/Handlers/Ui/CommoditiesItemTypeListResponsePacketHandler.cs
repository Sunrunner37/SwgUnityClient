using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Ui;

namespace Swg.Client.Core.Networking.Game.Handlers.Ui
{
    // Core3/MMOCoreORB/src/server/zone/packets/ui/CommoditiesItemTypeListResponse.h:11 (CommoditiesItemTypeListResponse).
    [Preserve]
    internal sealed class CommoditiesItemTypeListResponsePacketHandler : GamePacketHandler<CommoditiesItemTypeListResponsePacket>
    {
        public override uint Opcode => CommoditiesItemTypeListResponsePacket.Opcode;

        [Preserve]
        public CommoditiesItemTypeListResponsePacketHandler() : base(CommoditiesItemTypeListResponsePacket.ReadFrom)
        {
        }

        protected override void Handle(CommoditiesItemTypeListResponsePacket packet)
        {
            throw new NotImplementedException($"CommoditiesItemTypeListResponse (opcode 0x{Opcode:X8})");
        }
    }
}
