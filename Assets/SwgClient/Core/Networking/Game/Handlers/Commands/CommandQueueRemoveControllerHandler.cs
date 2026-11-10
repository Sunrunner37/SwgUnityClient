using Swg.Client.Core.Networking.Game.Packets.Commands;
using System;
using UnityEngine.Scripting;

namespace Swg.Client.Core.Networking.Game.Handlers.Commands
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/CommandQueueRemove.h:12 (CommandQueueRemove).
    [Preserve]
    internal sealed class CommandQueueRemoveControllerHandler : ObjectControllerHandler<CommandQueueRemovePacket>
    {
        public override uint ControllerType => CommandQueueRemovePacket.ControllerType;

        [Preserve]
        public CommandQueueRemoveControllerHandler() : base(CommandQueueRemovePacket.ReadFrom)
        {
        }

        protected override void Handle(CommandQueueRemovePacket packet)
        {
            throw new NotImplementedException($"CommandQueueRemove (controller 0x{ControllerType:X8})");
        }
    }
}
