using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Harvester
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/GenericResponse.h:13 (GenericResponse).
    public readonly struct GenericResponsePacket : IPacket
    {
        public const uint ControllerType = 0xee;
        public readonly uint Flags;
        public readonly ulong ReceiverId;
        public readonly uint EnvelopeReserved;
        public readonly uint Message;
        public readonly byte Result1;
        public readonly byte Result2;
        GenericResponsePacket(ObjectControllerPacket envelope)
        {
            this = default;
            if (envelope.ControllerType != ControllerType)
                throw new InvalidDataException("Unexpected GenericResponsePacket controller type");
            Flags = envelope.Flags;
            ReceiverId = envelope.ObjectId;
            EnvelopeReserved = envelope.Reserved;
            var reader = new ByteReader(envelope.Payload);
            Message = reader.U32();
            Result1 = reader.Byte();
            Result2 = reader.Byte();
            PacketFieldCodec.End(reader);
        }

        public static GenericResponsePacket ReadFrom(ObjectControllerPacket packet) => new GenericResponsePacket(packet);
        public static GenericResponsePacket ReadFrom(ByteReader reader) => ReadFrom(ObjectControllerPacket.ReadFrom(reader));
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(ObjectControllerPacket.OperandCount);
            writer.Write(ObjectControllerPacket.Opcode);
            writer.Write(Flags);
            writer.Write(ControllerType);
            writer.Write(ReceiverId);
            writer.Write(EnvelopeReserved);
            writer.Write(Message);
            writer.Write(Result1);
            writer.Write(Result2);
        }
    }
}
