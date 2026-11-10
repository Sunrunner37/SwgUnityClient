using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Combat;

namespace Swg.Client.Core.Networking.Game.Handlers.Combat
{
    // Core3/MMOCoreORB/src/server/zone/packets/tangible/UpdatePVPStatusMessage.h:11 (UpdatePVPStatusMessage).
    [Preserve]
    internal sealed class UpdatePVPStatusPacketHandler : GamePacketHandler<UpdatePVPStatusPacket>
    {
        public override uint Opcode => UpdatePVPStatusPacket.Opcode;

        [Preserve]
        public UpdatePVPStatusPacketHandler() : base(UpdatePVPStatusPacket.ReadFrom)
        {
        }

        protected override void Handle(UpdatePVPStatusPacket packet)
        {
            throw new NotImplementedException($"UpdatePVPStatusMessage (opcode 0x{Opcode:X8})");
        }
    }
}
