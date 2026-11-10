using Swg.Client.Core.Networking.Game.Packets.Effects;
using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Scene;

namespace Swg.Client.Core.Networking.Game.Handlers.Effects
{
    // Core3/MMOCoreORB/src/server/zone/packets/scene/PlayClientEventObjectMessage.h:15 (PlayClientEventObjectMessage).
    [Preserve]
    internal sealed class PlayClientEventObjectPacketHandler : GamePacketHandler<PlayClientEventObjectPacket>
    {
        public override uint Opcode => PlayClientEventObjectPacket.Opcode;

        [Preserve]
        public PlayClientEventObjectPacketHandler() : base(PlayClientEventObjectPacket.ReadFrom)
        {
        }

        protected override void Handle(PlayClientEventObjectPacket packet)
        {
            throw new NotImplementedException($"PlayClientEventObjectMessage (opcode 0x{Opcode:X8})");
        }
    }
}
