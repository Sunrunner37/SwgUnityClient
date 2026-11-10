using Swg.Client.Core.Networking.Game.Packets.Space;
using System;
using UnityEngine.Scripting;

namespace Swg.Client.Core.Networking.Game.Handlers.Space
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/JtlShipListResponse.h:15 (JtlShipListResponse).
    [Preserve]
    internal sealed class JtlShipListResponseControllerHandler : ObjectControllerHandler<JtlShipListResponsePacket>
    {
        public override uint ControllerType => JtlShipListResponsePacket.ControllerType;

        [Preserve]
        public JtlShipListResponseControllerHandler() : base(JtlShipListResponsePacket.ReadFrom)
        {
        }

        protected override void Handle(JtlShipListResponsePacket packet)
        {
            throw new NotImplementedException($"JtlShipListResponse (controller 0x{ControllerType:X8})");
        }
    }
}
