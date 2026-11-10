using Swg.Client.Core.Networking.Game.Packets.Support;
using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Ui;

namespace Swg.Client.Core.Networking.Game.Handlers.Support
{
    // Core3/MMOCoreORB/src/server/zone/packets/ui/GetArticleResponseMessage.h:10 (GetArticleResponseMessage).
    [Preserve]
    internal sealed class GetArticleResponsePacketHandler : GamePacketHandler<GetArticleResponsePacket>
    {
        public override uint Opcode => GetArticleResponsePacket.Opcode;

        [Preserve]
        public GetArticleResponsePacketHandler() : base(GetArticleResponsePacket.ReadFrom)
        {
        }

        protected override void Handle(GetArticleResponsePacket packet)
        {
            throw new NotImplementedException($"GetArticleResponseMessage (opcode 0x{Opcode:X8})");
        }
    }
}
