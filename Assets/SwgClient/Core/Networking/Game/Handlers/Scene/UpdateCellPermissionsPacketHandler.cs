using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Scene;

namespace Swg.Client.Core.Networking.Game.Handlers.Scene
{
    // Core3/MMOCoreORB/src/server/zone/packets/cell/UpdateCellPermissionsMessage.h:10 (UpdateCellPermissionsMessage).
    [Preserve]
    internal sealed class UpdateCellPermissionsPacketHandler : GamePacketHandler<UpdateCellPermissionsPacket>
    {
        public override uint Opcode => UpdateCellPermissionsPacket.Opcode;

        [Preserve]
        public UpdateCellPermissionsPacketHandler() : base(UpdateCellPermissionsPacket.ReadFrom)
        {
        }

        protected override void Handle(UpdateCellPermissionsPacket packet)
        {
            throw new NotImplementedException($"UpdateCellPermissionsMessage (opcode 0x{Opcode:X8})");
        }
    }
}
