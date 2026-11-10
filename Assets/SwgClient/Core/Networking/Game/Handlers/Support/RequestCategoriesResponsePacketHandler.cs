using Swg.Client.Core.Networking.Game.Packets.Support;
using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Ui;

namespace Swg.Client.Core.Networking.Game.Handlers.Support
{
    // Core3/MMOCoreORB/src/server/zone/packets/ui/RequestCategoriesResponseMessage.h:14 (RequestCategoriesResponseMessage).
    [Preserve]
    internal sealed class RequestCategoriesResponsePacketHandler : GamePacketHandler<RequestCategoriesResponsePacket>
    {
        public override uint Opcode => RequestCategoriesResponsePacket.Opcode;

        [Preserve]
        public RequestCategoriesResponsePacketHandler() : base(RequestCategoriesResponsePacket.ReadFrom)
        {
        }

        protected override void Handle(RequestCategoriesResponsePacket packet)
        {
            throw new NotImplementedException($"RequestCategoriesResponseMessage (opcode 0x{Opcode:X8})");
        }
    }
}
