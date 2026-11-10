using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Ui
{
    // Core3/MMOCoreORB/src/server/zone/packets/ui/ConGenericMessage.h:13 (ConGenericMessage).
    public readonly struct ConGenericPacket : IPacket
    {
        public const uint Opcode = 0x8c5fc76;
        public readonly ushort OperandCount;
        public readonly string Message;
        public readonly uint Reserved;
        ConGenericPacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 3))
                throw new InvalidDataException("Unexpected ConGenericPacket header");
            Message = reader.Ascii();
            Reserved = reader.U32();
            PacketFieldCodec.End(reader);
        }

        public static ConGenericPacket ReadFrom(ByteReader reader) => new ConGenericPacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            PacketFieldCodec.Ascii(writer, Message);
            writer.Write(Reserved);
        }
    }
}
