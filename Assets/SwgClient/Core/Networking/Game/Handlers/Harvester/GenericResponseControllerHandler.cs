using Swg.Client.Core.Networking.Game.Packets.Harvester;
using System;
using UnityEngine.Scripting;

namespace Swg.Client.Core.Networking.Game.Handlers.Harvester
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/GenericResponse.h:13 (GenericResponse).
    [Preserve]
    internal sealed class GenericResponseControllerHandler : ObjectControllerHandler<GenericResponsePacket>
    {
        public override uint ControllerType => GenericResponsePacket.ControllerType;

        [Preserve]
        public GenericResponseControllerHandler() : base(GenericResponsePacket.ReadFrom)
        {
        }

        protected override void Handle(GenericResponsePacket packet)
        {
            throw new NotImplementedException($"GenericResponse (controller 0x{ControllerType:X8})");
        }
    }
}
