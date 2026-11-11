using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Networking.Game.Packets.Data
{
    public readonly struct ChatToken
    {
        public readonly ulong SwgId;
        public readonly StringIdReference StringId;
        public readonly string Text;
        ChatToken(ByteReader reader)
        {
            this = default;
            SwgId = reader.U64();
            StringId = StringIdReference.ReadFrom(reader);
            Text = reader.Unicode();
        }

        public static ChatToken ReadFrom(ByteReader reader) => new ChatToken(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(SwgId);
            StringId.WriteTo(writer);
            PacketFieldCodec.Unicode(writer, Text);
        }
    }
}
