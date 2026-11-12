using Swg.Client.Core.Networking.Game.Packets.Commands;
using Swg.Client.Runtime.Networking;
using UnityEngine.Scripting;

namespace Swg.Client.Core.Networking.Game.Handlers.Commands
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/CommandQueueAdd.h:13 (CommandQueueAdd).
    [Preserve]
    internal sealed class CommandQueueAddControllerHandler : ObjectControllerHandler<CommandQueueAddPacket>
    {
        readonly ClientCommands commands;
        public override uint ControllerType => CommandQueueAddPacket.ControllerType;

        [Preserve]
        public CommandQueueAddControllerHandler(ClientCommands commands) : base(CommandQueueAddPacket.ReadFrom)
        {
            this.commands = commands;
        }

        protected override void Handle(CommandQueueAddPacket packet)
        {
            commands.Receive(packet);
        }
    }
}
