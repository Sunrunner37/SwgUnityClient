using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Effects
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/ShowFlyText.h:11 (ShowFlyText).
    public readonly struct ShowFlyTextPacket : IPacket
    {
        public const uint ControllerType = 0x1bd;
        public readonly uint Flags;
        public readonly ulong ReceiverId;
        public readonly uint EnvelopeReserved;
        public readonly ulong ObjectId;
        public readonly StringIdReference StringId;
        public readonly float Size;
        public readonly byte Red;
        public readonly byte Green;
        public readonly byte Blue;
        public readonly byte Reserved;
        ShowFlyTextPacket(ObjectControllerPacket envelope)
        {
            this = default;
            if (envelope.ControllerType != ControllerType)
                throw new InvalidDataException("Unexpected ShowFlyTextPacket controller type");
            Flags = envelope.Flags;
            ReceiverId = envelope.ObjectId;
            EnvelopeReserved = envelope.Reserved;
            var reader = new ByteReader(envelope.Payload);
            ObjectId = reader.U64();
            StringId = StringIdReference.ReadFrom(reader);
            Size = reader.Float();
            Red = reader.Byte();
            Green = reader.Byte();
            Blue = reader.Byte();
            Reserved = reader.Byte();
            PacketFieldCodec.End(reader);
        }

        public static ShowFlyTextPacket ReadFrom(ObjectControllerPacket packet) => new ShowFlyTextPacket(packet);
        public static ShowFlyTextPacket ReadFrom(ByteReader reader) => ReadFrom(ObjectControllerPacket.ReadFrom(reader));
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(ObjectControllerPacket.OperandCount);
            writer.Write(ObjectControllerPacket.Opcode);
            writer.Write(Flags);
            writer.Write(ControllerType);
            writer.Write(ReceiverId);
            writer.Write(EnvelopeReserved);
            writer.Write(ObjectId);
            StringId.WriteTo(writer);
            writer.Write(Size);
            writer.Write(Red);
            writer.Write(Green);
            writer.Write(Blue);
            writer.Write(Reserved);
        }
    }
}
