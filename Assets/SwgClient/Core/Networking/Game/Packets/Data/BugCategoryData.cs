using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Networking.Game.Packets.Data
{
    public readonly struct BugCategoryData
    {
        public readonly string Name;
        public readonly uint Id;
        public readonly BugCategoryData[] Children;
        public readonly byte Reserved1;
        public readonly byte Reserved2;
        BugCategoryData(ByteReader reader)
        {
            this = default;
            Name = reader.Unicode();
            Id = reader.U32();
            Children = new BugCategoryData[PacketFieldCodec.Count(reader, reader.U32(), 14)];
            for (int i = 0; i < Children.Length; i++)
                Children[i] = BugCategoryData.ReadFrom(reader);
            Reserved1 = reader.Byte();
            Reserved2 = reader.Byte();
        }

        public static BugCategoryData ReadFrom(ByteReader reader) => new BugCategoryData(reader);
        public void WriteTo(BinaryWriter writer)
        {
            PacketFieldCodec.Unicode(writer, Name);
            writer.Write(Id);
            writer.Write(checked((uint)Children.Length));
            foreach (var item in Children)
                item.WriteTo(writer);
            writer.Write(Reserved1);
            writer.Write(Reserved2);
        }
    }
}
