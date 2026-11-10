using Swg.Client.Core.Networking.Game.Packets.ImageDesign;
using System;
using UnityEngine.Scripting;

namespace Swg.Client.Core.Networking.Game.Handlers.ImageDesign
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/ImageDesignMessage.h:33 (ImageDesignChangeMessage).
    [Preserve]
    internal sealed class ImageDesignChangeControllerHandler : ObjectControllerHandler<ImageDesignChangePacket>
    {
        public override uint ControllerType => ImageDesignChangePacket.ControllerType;

        [Preserve]
        public ImageDesignChangeControllerHandler() : base(ImageDesignChangePacket.ReadFrom)
        {
        }

        protected override void Handle(ImageDesignChangePacket packet)
        {
            throw new NotImplementedException($"ImageDesignChangeMessage (controller 0x{ControllerType:X8})");
        }
    }
}
