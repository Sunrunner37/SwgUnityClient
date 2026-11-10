using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Support
{
    // Core3/MMOCoreORB/src/server/zone/packets/ui/SearchKnowledgebaseResponseMessage.h:10 (SearchKnowledgebaseResponseMessage).
    public readonly struct SearchKnowledgebaseResponsePacket : IPacket
    {
        public const uint Opcode = 0x7cbc8f67;
        public readonly ushort OperandCount;
        public readonly uint Result;
        public readonly ArticleSummary[] Articles;
        SearchKnowledgebaseResponsePacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 3))
                throw new InvalidDataException("Unexpected SearchKnowledgebaseResponsePacket header");
            Result = reader.U32();
            Articles = new ArticleSummary[PacketFieldCodec.Count(reader, reader.U32(), 8)];
            for (int i = 0; i < Articles.Length; i++)
                Articles[i] = ArticleSummary.ReadFrom(reader);
            PacketFieldCodec.End(reader);
        }

        public static SearchKnowledgebaseResponsePacket ReadFrom(ByteReader reader) => new SearchKnowledgebaseResponsePacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write(Result);
            writer.Write(checked((uint)Articles.Length));
            foreach (var item in Articles)
                item.WriteTo(writer);
        }
    }
}
