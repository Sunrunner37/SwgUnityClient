using Swg.Client.Core.Networking.Game.Packets.Support;
using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Ui;

namespace Swg.Client.Core.Networking.Game.Handlers.Support
{
    // Core3/MMOCoreORB/src/server/zone/packets/ui/AppendCommentsResponseMessage.h:10 (AppendCommentsResponseMessage).
    [Preserve]
    internal sealed class AppendCommentsResponsePacketHandler : GamePacketHandler<AppendCommentsResponsePacket>
    {
        public override uint Opcode => AppendCommentsResponsePacket.Opcode;

        [Preserve]
        public AppendCommentsResponsePacketHandler() : base(AppendCommentsResponsePacket.ReadFrom)
        {
        }

        protected override void Handle(AppendCommentsResponsePacket packet)
        {
            throw new NotImplementedException($"AppendCommentsResponseMessage (opcode 0x{Opcode:X8})");
        }
    }
}
