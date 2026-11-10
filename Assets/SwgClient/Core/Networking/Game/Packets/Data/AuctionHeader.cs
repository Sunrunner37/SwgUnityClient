using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Networking.Game.Packets.Data
{
    public readonly struct AuctionHeader
    {
        public readonly ulong ObjectId;
        public readonly byte NameIndex;
        public readonly int Price;
        public readonly uint SecondsRemaining;
        public readonly byte InstantSale;
        public readonly ushort VendorIndex;
        public readonly ulong OwnerId;
        public readonly ushort OwnerNameIndex;
        public readonly ulong BuyerId;
        public readonly ushort BuyerNameIndex;
        public readonly int ProxyPrice;
        public readonly int CurrentPrice;
        public readonly uint ItemType;
        public readonly uint Options;
        public readonly int AccessFee;
        AuctionHeader(ByteReader reader)
        {
            this = default;
            ObjectId = reader.U64();
            NameIndex = reader.Byte();
            Price = unchecked((int)reader.U32());
            SecondsRemaining = reader.U32();
            InstantSale = reader.Byte();
            VendorIndex = reader.U16();
            OwnerId = reader.U64();
            OwnerNameIndex = reader.U16();
            BuyerId = reader.U64();
            BuyerNameIndex = reader.U16();
            ProxyPrice = unchecked((int)reader.U32());
            CurrentPrice = unchecked((int)reader.U32());
            ItemType = reader.U32();
            Options = reader.U32();
            AccessFee = unchecked((int)reader.U32());
        }

        public static AuctionHeader ReadFrom(ByteReader reader) => new AuctionHeader(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(ObjectId);
            writer.Write(NameIndex);
            writer.Write(Price);
            writer.Write(SecondsRemaining);
            writer.Write(InstantSale);
            writer.Write(VendorIndex);
            writer.Write(OwnerId);
            writer.Write(OwnerNameIndex);
            writer.Write(BuyerId);
            writer.Write(BuyerNameIndex);
            writer.Write(ProxyPrice);
            writer.Write(CurrentPrice);
            writer.Write(ItemType);
            writer.Write(Options);
            writer.Write(AccessFee);
        }
    }
}
