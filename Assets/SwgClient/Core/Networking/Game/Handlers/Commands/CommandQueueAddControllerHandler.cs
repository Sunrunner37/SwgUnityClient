using Swg.Client.Core.Networking.Game.Packets.Commands;
using System;
using UnityEngine.Scripting;

namespace Swg.Client.Core.Networking.Game.Handlers.Commands
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/CommandQueueAdd.h:13 (CommandQueueAdd).
    [Preserve]
    internal sealed class CommandQueueAddControllerHandler : ObjectControllerHandler<CommandQueueAddPacket>
    {
        public override uint ControllerType => CommandQueueAddPacket.ControllerType;

        [Preserve]
        public CommandQueueAddControllerHandler() : base(CommandQueueAddPacket.ReadFrom)
        {
        }

        protected override void Handle(CommandQueueAddPacket packet)
        {
            throw new NotImplementedException($"CommandQueueAdd (controller 0x{ControllerType:X8})");
        }
    }
}
