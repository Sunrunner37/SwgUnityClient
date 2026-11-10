using Swg.Client.Core.Networking.Game.Packets.Combat;
using System;
using UnityEngine.Scripting;

namespace Swg.Client.Core.Networking.Game.Handlers.Combat
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/Buffs.h:27 (RemoveBuffMessage).
    [Preserve]
    internal sealed class RemoveBuffControllerHandler : ObjectControllerHandler<RemoveBuffPacket>
    {
        public override uint ControllerType => RemoveBuffPacket.ControllerType;

        [Preserve]
        public RemoveBuffControllerHandler() : base(RemoveBuffPacket.ReadFrom)
        {
        }

        protected override void Handle(RemoveBuffPacket packet)
        {
            throw new NotImplementedException($"RemoveBuffMessage (controller 0x{ControllerType:X8})");
        }
    }
}
