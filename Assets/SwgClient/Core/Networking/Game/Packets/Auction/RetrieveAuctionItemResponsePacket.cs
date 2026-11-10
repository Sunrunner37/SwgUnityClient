using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Auction
{
    // Core3/MMOCoreORB/src/server/zone/packets/auction/RetrieveAuctionItemResponseMessage.h:15 (RetrieveAuctionItemResponseMessage).
    public readonly struct RetrieveAuctionItemResponsePacket : IPacket
    {
        public const uint Opcode = 0x9499ef8c;
        public readonly ushort OperandCount;
        public readonly ulong ObjectId;
        public readonly uint Result;
        RetrieveAuctionItemResponsePacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 3))
                throw new InvalidDataException("Unexpected RetrieveAuctionItemResponsePacket header");
            ObjectId = reader.U64();
            Result = reader.U32();
            PacketFieldCodec.End(reader);
        }

        public static RetrieveAuctionItemResponsePacket ReadFrom(ByteReader reader) => new RetrieveAuctionItemResponsePacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write(ObjectId);
            writer.Write(Result);
        }
    }
}
