using System.IO;
using Swg.Client.Core.Networking.Game.Packets;

namespace Swg.Client.Core.Networking.Game.Packets.Commands
{
    /// <summary>Client-origin cancellation. Core3 CommandQueueRemoveCallback reads one counter/CRC pair after the count.</summary>
    public readonly struct CommandQueueDequeuePacket : IPacket
    {
        public const uint ControllerType = 0x117;
        public readonly ulong PlayerSwgId;
        public readonly uint ActionCounter;
        public readonly uint CommandCrc;
        public CommandQueueDequeuePacket(ulong playerSwgId, uint actionCounter, uint commandCrc)
        {
            PlayerSwgId = playerSwgId;
            ActionCounter = actionCounter;
            CommandCrc = commandCrc;
        }
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(ObjectControllerPacket.OperandCount);
            writer.Write(ObjectControllerPacket.Opcode);
            writer.Write(0x23u);
            writer.Write(ControllerType);
            writer.Write(PlayerSwgId);
            writer.Write(1u); // The controller's reserved word is the callback's pair count.
            writer.Write(ActionCounter);
            writer.Write(CommandCrc);
        }
    }
}
