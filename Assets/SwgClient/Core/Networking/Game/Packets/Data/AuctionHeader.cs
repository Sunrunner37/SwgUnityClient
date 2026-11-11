using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Networking.Game.Packets.Data
{
    public readonly struct AuctionHeader
    {
        public readonly ulong SwgId;
        public readonly byte NameIndex;
        public readonly int Price;
        public readonly uint SecondsRemaining;
        public readonly byte InstantSale;
        public readonly ushort VendorIndex;
        public readonly ulong OwnerSwgId;
        public readonly ushort OwnerNameIndex;
        public readonly ulong BuyerSwgId;
        public readonly ushort BuyerNameIndex;
        public readonly int ProxyPrice;
        public readonly int CurrentPrice;
        public readonly uint ItemType;
        public readonly uint Options;
        public readonly int AccessFee;
        AuctionHeader(ByteReader reader)
        {
            this = default;
            SwgId = reader.U64();
            NameIndex = reader.Byte();
            Price = unchecked((int)reader.U32());
            SecondsRemaining = reader.U32();
            InstantSale = reader.Byte();
            VendorIndex = reader.U16();
            OwnerSwgId = reader.U64();
            OwnerNameIndex = reader.U16();
            BuyerSwgId = reader.U64();
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
            writer.Write(SwgId);
            writer.Write(NameIndex);
            writer.Write(Price);
            writer.Write(SecondsRemaining);
            writer.Write(InstantSale);
            writer.Write(VendorIndex);
            writer.Write(OwnerSwgId);
            writer.Write(OwnerNameIndex);
            writer.Write(BuyerSwgId);
            writer.Write(BuyerNameIndex);
            writer.Write(ProxyPrice);
            writer.Write(CurrentPrice);
            writer.Write(ItemType);
            writer.Write(Options);
            writer.Write(AccessFee);
        }
    }
}
