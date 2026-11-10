using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Auction;

namespace Swg.Client.Core.Networking.Game.Handlers.Auction
{
    // Core3/MMOCoreORB/src/server/zone/packets/auction/IsVendorOwnerResponseMessage.h:13 (IsVendorOwnerResponseMessage).
    [Preserve]
    internal sealed class IsVendorOwnerResponsePacketHandler : GamePacketHandler<IsVendorOwnerResponsePacket>
    {
        public override uint Opcode => IsVendorOwnerResponsePacket.Opcode;

        [Preserve]
        public IsVendorOwnerResponsePacketHandler() : base(IsVendorOwnerResponsePacket.ReadFrom)
        {
        }

        protected override void Handle(IsVendorOwnerResponsePacket packet)
        {
            throw new NotImplementedException($"IsVendorOwnerResponseMessage (opcode 0x{Opcode:X8})");
        }
    }
}
