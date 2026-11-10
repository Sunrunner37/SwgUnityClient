using Swg.Client.Core.Networking.Game.Packets.ImageDesign;
using System;
using UnityEngine.Scripting;

namespace Swg.Client.Core.Networking.Game.Handlers.ImageDesign
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/ImageDesignMessage.h:22 (ImageDesignRejectMessage).
    [Preserve]
    internal sealed class ImageDesignRejectControllerHandler : ObjectControllerHandler<ImageDesignRejectPacket>
    {
        public override uint ControllerType => ImageDesignRejectPacket.ControllerType;

        [Preserve]
        public ImageDesignRejectControllerHandler() : base(ImageDesignRejectPacket.ReadFrom)
        {
        }

        protected override void Handle(ImageDesignRejectPacket packet)
        {
            throw new NotImplementedException($"ImageDesignRejectMessage (controller 0x{ControllerType:X8})");
        }
    }
}
