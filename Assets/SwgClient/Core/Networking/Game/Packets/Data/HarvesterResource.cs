using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Networking.Game.Packets.Data
{
    public readonly struct HarvesterResource
    {
        public readonly ulong SwgId;
        public readonly string Name;
        public readonly string Type;
        public readonly byte DensityPercent;
        HarvesterResource(ByteReader reader)
        {
            this = default;
            SwgId = reader.U64();
            Name = reader.Ascii();
            Type = reader.Ascii();
            DensityPercent = reader.Byte();
        }

        public static HarvesterResource ReadFrom(ByteReader reader) => new HarvesterResource(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(SwgId);
            PacketFieldCodec.Ascii(writer, Name);
            PacketFieldCodec.Ascii(writer, Type);
            writer.Write(DensityPercent);
        }
    }
}
