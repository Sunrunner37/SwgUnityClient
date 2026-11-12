using Swg.Client.Core.Networking.Game.Packets.Commands;
using Swg.Client.Runtime.Networking;
using UnityEngine.Scripting;

namespace Swg.Client.Core.Networking.Game.Handlers.Commands
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/CommandQueueRemove.h:12 (CommandQueueRemove).
    [Preserve]
    internal sealed class CommandQueueRemoveControllerHandler : ObjectControllerHandler<CommandQueueRemovePacket>
    {
        readonly ClientCommands commands;
        public override uint ControllerType => CommandQueueRemovePacket.ControllerType;

        [Preserve]
        public CommandQueueRemoveControllerHandler(ClientCommands commands) : base(CommandQueueRemovePacket.ReadFrom)
        {
            this.commands = commands;
        }

        protected override void Handle(CommandQueueRemovePacket packet)
        {
            commands.Receive(packet);
        }
    }
}
