using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Trade
{
    // Core3/MMOCoreORB/src/server/zone/packets/trade/AddItemMessage.h:8 (AddItemMessage).
    public readonly struct AddItemPacket : IPacket
    {
        public const uint Opcode = 0x1e8d1356;
        public readonly ushort OperandCount;
        public readonly ulong ItemId;
        AddItemPacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 1))
                throw new InvalidDataException("Unexpected AddItemPacket header");
            ItemId = reader.U64();
            PacketFieldCodec.End(reader);
        }

        public static AddItemPacket ReadFrom(ByteReader reader) => new AddItemPacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write(ItemId);
        }
    }
}
