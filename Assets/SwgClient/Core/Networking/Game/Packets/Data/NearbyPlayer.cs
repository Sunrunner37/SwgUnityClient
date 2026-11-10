using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Networking.Game.Packets.Data
{
    public readonly struct NearbyPlayer
    {
        public readonly uint[] Bitmasks;
        public readonly string Name;
        public readonly uint Species;
        public readonly string Region;
        public readonly string Planet;
        public readonly string Guild;
        public readonly string Title;
        NearbyPlayer(ByteReader reader)
        {
            this = default;
            Bitmasks = new uint[PacketFieldCodec.Count(reader, reader.U32(), 4)];
            for (int i = 0; i < Bitmasks.Length; i++)
                Bitmasks[i] = reader.U32();
            Name = reader.Unicode();
            Species = reader.U32();
            Region = reader.Ascii();
            Planet = reader.Ascii();
            Guild = reader.Ascii();
            Title = reader.Ascii();
        }

        public static NearbyPlayer ReadFrom(ByteReader reader) => new NearbyPlayer(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(checked((uint)Bitmasks.Length));
            foreach (var item in Bitmasks)
                writer.Write(item);
            PacketFieldCodec.Unicode(writer, Name);
            writer.Write(Species);
            PacketFieldCodec.Ascii(writer, Region);
            PacketFieldCodec.Ascii(writer, Planet);
            PacketFieldCodec.Ascii(writer, Guild);
            PacketFieldCodec.Ascii(writer, Title);
        }
    }
}
