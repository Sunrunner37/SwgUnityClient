using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Auction
{
    // Core3/MMOCoreORB/src/server/zone/packets/auction/AuctionItemDescriptionMessage.h:10 (AuctionItemDescriptionMessage).
    // Core3/MMOCoreORB/src/server/zone/packets/auction/GetAuctionDetailsResponse.h:6 (GetAuctionDetailsResponse).
    // Core3/MMOCoreORB/src/server/zone/packets/scene/AttributeListMessage.h:17 (AttributeListMessage).
    public readonly struct AuctionItemDescriptionPacket : IPacket
    {
        public const uint Opcode = 0xfe0e644b;
        public readonly ushort OperandCount;
        public readonly ulong ObjectId;
        public readonly string Description;
        public readonly AttributeValue[] Attributes;
        public readonly uint? Reserved;
        AuctionItemDescriptionPacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 2))
                throw new InvalidDataException("Unexpected AuctionItemDescriptionPacket header");
            ObjectId = reader.U64();
            Description = reader.Unicode();
            Attributes = new AttributeValue[PacketFieldCodec.Count(reader, reader.U32(), 6)];
            for (int i = 0; i < Attributes.Length; i++)
                Attributes[i] = AttributeValue.ReadFrom(reader);
            Reserved = reader.Remaining == 0 ? (uint? )null : reader.U32();
            PacketFieldCodec.End(reader);
        }

        public static AuctionItemDescriptionPacket ReadFrom(ByteReader reader) => new AuctionItemDescriptionPacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write(ObjectId);
            PacketFieldCodec.Unicode(writer, Description);
            writer.Write(checked((uint)Attributes.Length));
            foreach (var item in Attributes)
                item.WriteTo(writer);
            if (Reserved.HasValue)
                writer.Write(Reserved.Value);
        }
    }
}
