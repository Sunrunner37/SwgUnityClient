using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Scene;

namespace Swg.Client.Core.Networking.Game.Handlers.Scene
{
    // Core3/MMOCoreORB/src/server/zone/packets/scene/ServerWeatherMessage.h:10 (ServerWeatherMessage).
    [Preserve]
    internal sealed class ServerWeatherPacketHandler : GamePacketHandler<ServerWeatherPacket>
    {
        public override uint Opcode => ServerWeatherPacket.Opcode;

        [Preserve]
        public ServerWeatherPacketHandler() : base(ServerWeatherPacket.ReadFrom)
        {
        }

        protected override void Handle(ServerWeatherPacket packet)
        {
            throw new NotImplementedException($"ServerWeatherMessage (opcode 0x{Opcode:X8})");
        }
    }
}
