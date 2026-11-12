using System.IO;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Commands
{
    // Core3 object/CommandQueueEnqueue.cpp: enqueue a command on the player's controller.
    public readonly struct CommandQueueEnqueuePacket : IPacket
    {
        public const uint ControllerType = 0x116;
        public readonly ulong PlayerSwgId;
        public readonly uint Sequence;
        public readonly uint CommandCrc;
        public readonly ulong TargetSwgId;
        public readonly string Arguments;
        public CommandQueueEnqueuePacket(ulong playerSwgId, uint sequence, uint commandCrc, string arguments, ulong targetSwgId = 0)
        {
            PlayerSwgId = playerSwgId;
            Sequence = sequence;
            CommandCrc = commandCrc;
            TargetSwgId = targetSwgId;
            Arguments = arguments;
        }

        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(ObjectControllerPacket.OperandCount);
            writer.Write(ObjectControllerPacket.Opcode);
            writer.Write(0x23u);
            writer.Write(ControllerType);
            writer.Write(PlayerSwgId);
            writer.Write(0u);
            writer.Write(Sequence);
            writer.Write(CommandCrc);
            writer.Write(TargetSwgId);
            PacketFieldCodec.Unicode(writer, Arguments);
        }
    }
}
