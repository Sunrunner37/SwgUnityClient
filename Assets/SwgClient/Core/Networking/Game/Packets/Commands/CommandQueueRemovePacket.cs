using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Commands
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/CommandQueueRemove.h:12 (CommandQueueRemove).
    public readonly struct CommandQueueRemovePacket : IPacket
    {
        public const uint ControllerType = 0x117;
        public readonly uint Flags;
        public readonly ulong ReceiverId;
        public readonly uint EnvelopeReserved;
        public readonly uint ActionCounter;
        public readonly float Timer;
        public readonly uint ErrorType;
        public readonly uint ErrorCode;
        CommandQueueRemovePacket(ObjectControllerPacket envelope)
        {
            this = default;
            if (envelope.ControllerType != ControllerType)
                throw new InvalidDataException("Unexpected CommandQueueRemovePacket controller type");
            Flags = envelope.Flags;
            ReceiverId = envelope.ObjectId;
            EnvelopeReserved = envelope.Reserved;
            var reader = new ByteReader(envelope.Payload);
            ActionCounter = reader.U32();
            Timer = reader.Float();
            ErrorType = reader.U32();
            ErrorCode = reader.U32();
            PacketFieldCodec.End(reader);
        }

        public static CommandQueueRemovePacket ReadFrom(ObjectControllerPacket packet) => new CommandQueueRemovePacket(packet);
        public static CommandQueueRemovePacket ReadFrom(ByteReader reader) => ReadFrom(ObjectControllerPacket.ReadFrom(reader));
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(ObjectControllerPacket.OperandCount);
            writer.Write(ObjectControllerPacket.Opcode);
            writer.Write(Flags);
            writer.Write(ControllerType);
            writer.Write(ReceiverId);
            writer.Write(EnvelopeReserved);
            writer.Write(ActionCounter);
            writer.Write(Timer);
            writer.Write(ErrorType);
            writer.Write(ErrorCode);
        }
    }
}
