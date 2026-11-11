using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Auction
{
    // Core3/MMOCoreORB/src/server/zone/packets/auction/CancelLiveAuctionResponseMessage.h:17 (CancelLiveAuctionResponseMessage).
    public readonly struct CancelLiveAuctionResponsePacket : IPacket
    {
        public const uint Opcode = 0x7da2246c;
        public readonly ushort OperandCount;
        public readonly ulong SwgId;
        public readonly uint Result;
        public readonly byte Reserved;
        CancelLiveAuctionResponsePacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 4))
                throw new InvalidDataException("Unexpected CancelLiveAuctionResponsePacket header");
            SwgId = reader.U64();
            Result = reader.U32();
            Reserved = reader.Byte();
            PacketFieldCodec.End(reader);
        }

        public static CancelLiveAuctionResponsePacket ReadFrom(ByteReader reader) => new CancelLiveAuctionResponsePacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write(SwgId);
            writer.Write(Result);
            writer.Write(Reserved);
        }
    }
}
