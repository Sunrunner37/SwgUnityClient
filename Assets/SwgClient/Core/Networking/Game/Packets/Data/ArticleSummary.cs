using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Networking.Game.Packets.Data
{
    public readonly struct ArticleSummary
    {
        public readonly string Title;
        public readonly string ArticleId;
        public readonly ushort Reserved;
        ArticleSummary(ByteReader reader)
        {
            this = default;
            Title = reader.Unicode();
            ArticleId = reader.Ascii();
            Reserved = reader.U16();
        }

        public static ArticleSummary ReadFrom(ByteReader reader) => new ArticleSummary(reader);
        public void WriteTo(BinaryWriter writer)
        {
            PacketFieldCodec.Unicode(writer, Title);
            PacketFieldCodec.Ascii(writer, ArticleId);
            writer.Write(Reserved);
        }
    }
}
