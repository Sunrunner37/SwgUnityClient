using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Commands
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/CommandQueueAdd.h:13 (CommandQueueAdd).
    public readonly struct CommandQueueAddPacket : IPacket
    {
        public const uint ControllerType = 0x167;
        public readonly uint Flags;
        public readonly ulong ReceiverSwgId;
        public readonly uint EnvelopeReserved;
        public readonly uint ActionCounter;
        public readonly uint ActionCrc;
        CommandQueueAddPacket(ObjectControllerPacket envelope)
        {
            this = default;
            if (envelope.ControllerType != ControllerType)
                throw new InvalidDataException("Unexpected CommandQueueAddPacket controller type");
            Flags = envelope.Flags;
            ReceiverSwgId = envelope.SwgId;
            EnvelopeReserved = envelope.Reserved;
            var reader = new ByteReader(envelope.Payload);
            ActionCounter = reader.U32();
            ActionCrc = reader.U32();
            PacketFieldCodec.End(reader);
        }

        public static CommandQueueAddPacket ReadFrom(ObjectControllerPacket packet) => new CommandQueueAddPacket(packet);
        public static CommandQueueAddPacket ReadFrom(ByteReader reader) => ReadFrom(ObjectControllerPacket.ReadFrom(reader));
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(ObjectControllerPacket.OperandCount);
            writer.Write(ObjectControllerPacket.Opcode);
            writer.Write(Flags);
            writer.Write(ControllerType);
            writer.Write(ReceiverSwgId);
            writer.Write(EnvelopeReserved);
            writer.Write(ActionCounter);
            writer.Write(ActionCrc);
        }
    }
}
