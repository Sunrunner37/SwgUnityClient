using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Support
{
    // Core3/MMOCoreORB/src/server/zone/packets/ui/GetArticleResponseMessage.h:10 (GetArticleResponseMessage).
    public readonly struct GetArticleResponsePacket : IPacket
    {
        public const uint Opcode = 0x934baee0;
        public readonly ushort OperandCount;
        public readonly uint Result;
        public readonly string Article;
        GetArticleResponsePacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 3))
                throw new InvalidDataException("Unexpected GetArticleResponsePacket header");
            Result = reader.U32();
            Article = reader.Unicode();
            PacketFieldCodec.End(reader);
        }

        public static GetArticleResponsePacket ReadFrom(ByteReader reader) => new GetArticleResponsePacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write(Result);
            PacketFieldCodec.Unicode(writer, Article);
        }
    }
}
