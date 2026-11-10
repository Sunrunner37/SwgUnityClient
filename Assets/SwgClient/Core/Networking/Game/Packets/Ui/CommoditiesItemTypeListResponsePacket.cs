using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Ui
{
    // Core3/MMOCoreORB/src/server/zone/packets/ui/CommoditiesItemTypeListResponse.h:11 (CommoditiesItemTypeListResponse).
    public readonly struct CommoditiesItemTypeListResponsePacket : IPacket
    {
        public const uint Opcode = 0xd4e937fc;
        public readonly ushort OperandCount;
        public readonly string Version;
        public readonly uint Reserved;
        CommoditiesItemTypeListResponsePacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 2))
                throw new InvalidDataException("Unexpected CommoditiesItemTypeListResponsePacket header");
            Version = reader.Ascii();
            Reserved = reader.U32();
            PacketFieldCodec.End(reader);
        }

        public static CommoditiesItemTypeListResponsePacket ReadFrom(ByteReader reader) => new CommoditiesItemTypeListResponsePacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            PacketFieldCodec.Ascii(writer, Version);
            writer.Write(Reserved);
        }
    }
}
