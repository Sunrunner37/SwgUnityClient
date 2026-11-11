using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Auction
{
    // Core3/MMOCoreORB/src/server/zone/packets/auction/BidAuctionResponseMessage.h:19 (BidAuctionResponseMessage).
    public readonly struct BidAuctionResponsePacket : IPacket
    {
        public const uint Opcode = 0x8fcbef4a;
        public readonly ushort OperandCount;
        public readonly ulong SwgId;
        public readonly uint Result;
        BidAuctionResponsePacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 3))
                throw new InvalidDataException("Unexpected BidAuctionResponsePacket header");
            SwgId = reader.U64();
            Result = reader.U32();
            PacketFieldCodec.End(reader);
        }

        public static BidAuctionResponsePacket ReadFrom(ByteReader reader) => new BidAuctionResponsePacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write(SwgId);
            writer.Write(Result);
        }
    }
}
