using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Networking.Game.Packets.Data
{
    public readonly struct StringIdReference
    {
        public readonly string File;
        public readonly uint Reserved;
        public readonly string Key;
        StringIdReference(ByteReader reader)
        {
            this = default;
            File = reader.Ascii();
            Reserved = reader.U32();
            Key = reader.Ascii();
        }

        public static StringIdReference ReadFrom(ByteReader reader) => new StringIdReference(reader);
        public void WriteTo(BinaryWriter writer)
        {
            PacketFieldCodec.Ascii(writer, File);
            writer.Write(Reserved);
            PacketFieldCodec.Ascii(writer, Key);
        }
    }
}
