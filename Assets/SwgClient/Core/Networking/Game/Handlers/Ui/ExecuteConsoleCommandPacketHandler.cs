using System;
using UnityEngine.Scripting;
using Swg.Client.Core.Networking.Game.Packets.Ui;

namespace Swg.Client.Core.Networking.Game.Handlers.Ui
{
    // Core3/MMOCoreORB/src/server/zone/packets/ui/ExecuteConsoleCommand.h:11 (ExecuteConsoleCommand).
    [Preserve]
    internal sealed class ExecuteConsoleCommandPacketHandler : GamePacketHandler<ExecuteConsoleCommandPacket>
    {
        public override uint Opcode => ExecuteConsoleCommandPacket.Opcode;

        [Preserve]
        public ExecuteConsoleCommandPacketHandler() : base(ExecuteConsoleCommandPacket.ReadFrom)
        {
        }

        protected override void Handle(ExecuteConsoleCommandPacket packet)
        {
            throw new NotImplementedException($"ExecuteConsoleCommand (opcode 0x{Opcode:X8})");
        }
    }
}
