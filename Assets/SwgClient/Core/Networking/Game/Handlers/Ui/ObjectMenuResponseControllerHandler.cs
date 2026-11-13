using Swg.Client.Runtime.Networking;
using Swg.Client.Core.Networking.Game.Packets.Ui;
using System;
using UnityEngine.Scripting;

namespace Swg.Client.Core.Networking.Game.Handlers.Ui
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/ObjectMenuResponse.h:17 (ObjectMenuResponse).
    [Preserve]
    internal sealed class ObjectMenuResponseControllerHandler : ObjectControllerHandler<ObjectMenuResponsePacket>
    {
        readonly ClientObjectInteractions interactions;
        public override uint ControllerType => ObjectMenuResponsePacket.ControllerType;

        [Preserve]
        public ObjectMenuResponseControllerHandler(ClientObjectInteractions interactions) : base(ObjectMenuResponsePacket.ReadFrom)
        {
            this.interactions = interactions;
        }

        protected override void Handle(ObjectMenuResponsePacket packet)
        {
            interactions.Receive(packet);
        }
    }
}
