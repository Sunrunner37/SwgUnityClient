using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Ui;

namespace Swg.Client.Core.Networking.Game.Handlers.Ui
{
    // Core3/MMOCoreORB/src/server/zone/packets/ui/DestroyClientPathMessage.h:11 (DestroyClientPathMessage).
    [Preserve]
    internal sealed class DestroyClientPathPacketHandler : GamePacketHandler<DestroyClientPathPacket>
    {
        public override uint Opcode => DestroyClientPathPacket.Opcode;

        [Preserve]
        public DestroyClientPathPacketHandler() : base(DestroyClientPathPacket.ReadFrom)
        {
        }

        protected override void Handle(DestroyClientPathPacket packet)
        {
            throw new NotImplementedException($"DestroyClientPathMessage (opcode 0x{Opcode:X8})");
        }
    }
}
