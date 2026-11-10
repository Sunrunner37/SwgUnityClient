using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Auction
{
    // Core3/MMOCoreORB/src/server/zone/packets/auction/AuctionQueryHeadersResponseMessage.h:14 (AuctionQueryHeadersResponseMessage).
    public readonly struct AuctionQueryHeadersResponsePacket : IPacket
    {
        public const uint Opcode = 0xfa500e52;
        public readonly ushort OperandCount;
        public readonly uint Counter;
        public readonly uint Screen;
        public readonly string[] Locations;
        public readonly string[] ItemNames;
        public readonly AuctionHeader[] Items;
        public readonly ushort Offset;
        public readonly byte Continues;
        AuctionQueryHeadersResponsePacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 8))
                throw new InvalidDataException("Unexpected AuctionQueryHeadersResponsePacket header");
            Counter = reader.U32();
            Screen = reader.U32();
            Locations = new string[PacketFieldCodec.Count(reader, reader.U32(), 2)];
            for (int i = 0; i < Locations.Length; i++)
                Locations[i] = reader.Ascii();
            ItemNames = new string[PacketFieldCodec.Count(reader, reader.U32(), 4)];
            for (int i = 0; i < ItemNames.Length; i++)
                ItemNames[i] = reader.Unicode();
            Items = new AuctionHeader[PacketFieldCodec.Count(reader, reader.U32(), 60)];
            for (int i = 0; i < Items.Length; i++)
                Items[i] = AuctionHeader.ReadFrom(reader);
            Offset = reader.U16();
            Continues = reader.Byte();
            PacketFieldCodec.End(reader);
        }

        public static AuctionQueryHeadersResponsePacket ReadFrom(ByteReader reader) => new AuctionQueryHeadersResponsePacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write(Counter);
            writer.Write(Screen);
            writer.Write(checked((uint)Locations.Length));
            foreach (var item in Locations)
                PacketFieldCodec.Ascii(writer, item);
            writer.Write(checked((uint)ItemNames.Length));
            foreach (var item in ItemNames)
                PacketFieldCodec.Unicode(writer, item);
            writer.Write(checked((uint)Items.Length));
            foreach (var item in Items)
                item.WriteTo(writer);
            writer.Write(Offset);
            writer.Write(Continues);
        }
    }
}
