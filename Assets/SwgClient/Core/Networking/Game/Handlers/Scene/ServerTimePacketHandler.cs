using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Scene;

namespace Swg.Client.Core.Networking.Game.Handlers.Scene
{
    // Core3/MMOCoreORB/src/server/zone/packets/scene/ServerTimeMessage.h:12 (ServerTimeMessage).
    [Preserve]
    internal sealed class ServerTimePacketHandler : GamePacketHandler<ServerTimePacket>
    {
        public override uint Opcode => ServerTimePacket.Opcode;

        [Preserve]
        public ServerTimePacketHandler() : base(ServerTimePacket.ReadFrom)
        {
        }

        protected override void Handle(ServerTimePacket packet)
        {
            throw new NotImplementedException($"ServerTimeMessage (opcode 0x{Opcode:X8})");
        }
    }
}
