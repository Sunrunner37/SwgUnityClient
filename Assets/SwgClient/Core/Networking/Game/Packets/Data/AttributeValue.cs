using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Networking.Game.Packets.Data
{
    public readonly struct AttributeValue
    {
        public readonly string Name;
        public readonly string Value;
        AttributeValue(ByteReader reader)
        {
            this = default;
            Name = reader.Ascii();
            Value = reader.Unicode();
        }

        public static AttributeValue ReadFrom(ByteReader reader) => new AttributeValue(reader);
        public void WriteTo(BinaryWriter writer)
        {
            PacketFieldCodec.Ascii(writer, Name);
            PacketFieldCodec.Unicode(writer, Value);
        }
    }
}
