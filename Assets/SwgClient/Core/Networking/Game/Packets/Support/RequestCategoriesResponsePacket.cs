using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Support
{
    // Core3/MMOCoreORB/src/server/zone/packets/ui/RequestCategoriesResponseMessage.h:14 (RequestCategoriesResponseMessage).
    public readonly struct RequestCategoriesResponsePacket : IPacket
    {
        public const uint Opcode = 0x61148fd4;
        public readonly ushort OperandCount;
        public readonly uint Result;
        public readonly BugCategoryData[] Categories;
        RequestCategoriesResponsePacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 3))
                throw new InvalidDataException("Unexpected RequestCategoriesResponsePacket header");
            Result = reader.U32();
            Categories = new BugCategoryData[PacketFieldCodec.Count(reader, reader.U32(), 14)];
            for (int i = 0; i < Categories.Length; i++)
                Categories[i] = BugCategoryData.ReadFrom(reader);
            PacketFieldCodec.End(reader);
        }

        public static RequestCategoriesResponsePacket ReadFrom(ByteReader reader) => new RequestCategoriesResponsePacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write(Result);
            writer.Write(checked((uint)Categories.Length));
            foreach (var item in Categories)
                item.WriteTo(writer);
        }
    }
}
