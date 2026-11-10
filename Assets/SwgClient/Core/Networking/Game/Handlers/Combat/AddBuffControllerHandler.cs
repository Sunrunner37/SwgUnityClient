using Swg.Client.Core.Networking.Game.Packets.Combat;
using System;
using UnityEngine.Scripting;

namespace Swg.Client.Core.Networking.Game.Handlers.Combat
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/Buffs.h:10 (AddBuffMessage).
    [Preserve]
    internal sealed class AddBuffControllerHandler : ObjectControllerHandler<AddBuffPacket>
    {
        public override uint ControllerType => AddBuffPacket.ControllerType;

        [Preserve]
        public AddBuffControllerHandler() : base(AddBuffPacket.ReadFrom)
        {
        }

        protected override void Handle(AddBuffPacket packet)
        {
            throw new NotImplementedException($"AddBuffMessage (controller 0x{ControllerType:X8})");
        }
    }
}
