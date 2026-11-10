using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Auction
{
    // Core3/MMOCoreORB/src/server/zone/packets/auction/AcceptAuctionResponseMessage.h:13 (AcceptAuctionResponseMessage).
    public readonly struct AcceptAuctionResponsePacket : IPacket
    {
        public const uint Opcode = 0xc58a446e;
        public readonly ushort OperandCount;
        public readonly ulong ObjectId;
        public readonly uint Result;
        AcceptAuctionResponsePacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 3))
                throw new InvalidDataException("Unexpected AcceptAuctionResponsePacket header");
            ObjectId = reader.U64();
            Result = reader.U32();
            PacketFieldCodec.End(reader);
        }

        public static AcceptAuctionResponsePacket ReadFrom(ByteReader reader) => new AcceptAuctionResponsePacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write(ObjectId);
            writer.Write(Result);
        }
    }
}
