using Swg.Client.Core.Networking.Game.Packets.Ui;
using System;
using UnityEngine.Scripting;

namespace Swg.Client.Core.Networking.Game.Handlers.Ui
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/ObjectMenuResponse.h:17 (ObjectMenuResponse).
    [Preserve]
    internal sealed class ObjectMenuResponseControllerHandler : ObjectControllerHandler<ObjectMenuResponsePacket>
    {
        public override uint ControllerType => ObjectMenuResponsePacket.ControllerType;

        [Preserve]
        public ObjectMenuResponseControllerHandler() : base(ObjectMenuResponsePacket.ReadFrom)
        {
        }

        protected override void Handle(ObjectMenuResponsePacket packet)
        {
            throw new NotImplementedException($"ObjectMenuResponse (controller 0x{ControllerType:X8})");
        }
    }
}
