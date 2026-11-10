using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Scene;

namespace Swg.Client.Core.Networking.Game.Handlers.Scene
{
    // Core3/MMOCoreORB/src/server/zone/packets/scene/GameSceneChangedMessage.h:10 (GameSceneChangedMessage).
    [Preserve]
    internal sealed class GameSceneChangedPacketHandler : GamePacketHandler<GameSceneChangedPacket>
    {
        public override uint Opcode => GameSceneChangedPacket.Opcode;

        [Preserve]
        public GameSceneChangedPacketHandler() : base(GameSceneChangedPacket.ReadFrom)
        {
        }

        protected override void Handle(GameSceneChangedPacket packet)
        {
            throw new NotImplementedException($"GameSceneChangedMessage (opcode 0x{Opcode:X8})");
        }
    }
}
