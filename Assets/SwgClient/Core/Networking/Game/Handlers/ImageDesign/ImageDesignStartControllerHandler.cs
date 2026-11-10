using Swg.Client.Core.Networking.Game.Packets.ImageDesign;
using System;
using UnityEngine.Scripting;

namespace Swg.Client.Core.Networking.Game.Handlers.ImageDesign
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/ImageDesignMessage.h:11 (ImageDesignStartMessage).
    [Preserve]
    internal sealed class ImageDesignStartControllerHandler : ObjectControllerHandler<ImageDesignStartPacket>
    {
        public override uint ControllerType => ImageDesignStartPacket.ControllerType;

        [Preserve]
        public ImageDesignStartControllerHandler() : base(ImageDesignStartPacket.ReadFrom)
        {
        }

        protected override void Handle(ImageDesignStartPacket packet)
        {
            throw new NotImplementedException($"ImageDesignStartMessage (controller 0x{ControllerType:X8})");
        }
    }
}
