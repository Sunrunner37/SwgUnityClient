using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Ui;

namespace Swg.Client.Core.Networking.Game.Handlers.Ui
{
    // Core3/MMOCoreORB/src/server/zone/packets/ui/PermissionListCreateMessage.h:8 (PermissionListCreateMessage).
    [Preserve]
    internal sealed class PermissionListCreatePacketHandler : GamePacketHandler<PermissionListCreatePacket>
    {
        public override uint Opcode => PermissionListCreatePacket.Opcode;

        [Preserve]
        public PermissionListCreatePacketHandler() : base(PermissionListCreatePacket.ReadFrom)
        {
        }

        protected override void Handle(PermissionListCreatePacket packet)
        {
            throw new NotImplementedException($"PermissionListCreateMessage (opcode 0x{Opcode:X8})");
        }
    }
}
