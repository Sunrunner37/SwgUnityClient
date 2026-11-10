using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Player;

namespace Swg.Client.Core.Networking.Game.Handlers.Player
{
    // Core3/MMOCoreORB/src/server/zone/packets/player/GetMapLocationsResponseMessage.h:12 (GetMapLocationsResponseMessage).
    [Preserve]
    internal sealed class GetMapLocationsResponsePacketHandler : GamePacketHandler<GetMapLocationsResponsePacket>
    {
        public override uint Opcode => GetMapLocationsResponsePacket.Opcode;

        [Preserve]
        public GetMapLocationsResponsePacketHandler() : base(GetMapLocationsResponsePacket.ReadFrom)
        {
        }

        protected override void Handle(GetMapLocationsResponsePacket packet)
        {
            throw new NotImplementedException($"GetMapLocationsResponseMessage (opcode 0x{Opcode:X8})");
        }
    }
}
