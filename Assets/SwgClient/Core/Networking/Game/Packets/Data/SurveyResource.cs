using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Networking.Game.Packets.Data
{
    public readonly struct SurveyResource
    {
        public readonly string Name;
        public readonly ulong ObjectId;
        public readonly string Type;
        SurveyResource(ByteReader reader)
        {
            this = default;
            Name = reader.Ascii();
            ObjectId = reader.U64();
            Type = reader.Ascii();
        }

        public static SurveyResource ReadFrom(ByteReader reader) => new SurveyResource(reader);
        public void WriteTo(BinaryWriter writer)
        {
            PacketFieldCodec.Ascii(writer, Name);
            writer.Write(ObjectId);
            PacketFieldCodec.Ascii(writer, Type);
        }
    }
}
