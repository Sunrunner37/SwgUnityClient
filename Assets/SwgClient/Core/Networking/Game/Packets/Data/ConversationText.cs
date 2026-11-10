using System.IO;
using System.Text;
using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Networking.Game.Packets.Data
{
    public readonly struct ConversationText
    {
        public readonly string Text;
        public readonly ushort Reserved;
        public readonly ChatParameterData? Localized;
        ConversationText(ByteReader reader)
        {
            this = default;
            uint units = reader.U32();
            var body = new ByteReader(reader.Take(checked((int)units * 2)));
            if (body.Remaining >= 9 && body.Bytes[0] == 0 && body.Bytes[1] == 0 && ChatParameterData.IsHeader(body, 2))
            {
                Reserved = body.U16();
                Localized = ChatParameterData.ReadFrom(body);
                PacketFieldCodec.End(body);
            }
            else
                Text = Encoding.Unicode.GetString(body.Bytes);
        }

        public static ConversationText ReadFrom(ByteReader reader) => new ConversationText(reader);
        public void WriteTo(BinaryWriter writer)
        {
            if (!Localized.HasValue)
            {
                PacketFieldCodec.Unicode(writer, Text);
                return;
            }

            using var stream = new MemoryStream();
            using var body = new BinaryWriter(stream);
            body.Write(Reserved);
            Localized.Value.WriteTo(body);
            writer.Write(checked((uint)(stream.Length / 2)));
            writer.Write(stream.ToArray());
        }
    }
}
