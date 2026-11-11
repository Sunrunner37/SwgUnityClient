using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Networking.Game.Packets.Data
{
    public readonly struct MapLocation
    {
        public readonly ulong SwgId;
        public readonly string Name;
        public readonly float X;
        public readonly float Y;
        public readonly byte Category;
        public readonly byte Subcategory;
        public readonly byte Icon;
        MapLocation(ByteReader reader)
        {
            this = default;
            SwgId = reader.U64();
            Name = reader.Unicode();
            X = reader.Float();
            Y = reader.Float();
            Category = reader.Byte();
            Subcategory = reader.Byte();
            Icon = reader.Byte();
        }

        public static MapLocation ReadFrom(ByteReader reader) => new MapLocation(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(SwgId);
            PacketFieldCodec.Unicode(writer, Name);
            writer.Write(X);
            writer.Write(Y);
            writer.Write(Category);
            writer.Write(Subcategory);
            writer.Write(Icon);
        }
    }
}
