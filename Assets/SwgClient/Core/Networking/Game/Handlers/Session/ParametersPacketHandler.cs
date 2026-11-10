using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Session;

namespace Swg.Client.Core.Networking.Game.Handlers.Session
{
    // Core3/MMOCoreORB/src/server/zone/packets/zone/ParametersMessage.h:12 (ParametersMessage).
    [Preserve]
    internal sealed class ParametersPacketHandler : GamePacketHandler<ParametersPacket>
    {
        public override uint Opcode => ParametersPacket.Opcode;

        [Preserve]
        public ParametersPacketHandler() : base(ParametersPacket.ReadFrom)
        {
        }

        protected override void Handle(ParametersPacket packet)
        {
            throw new NotImplementedException($"ParametersMessage (opcode 0x{Opcode:X8})");
        }
    }
}
