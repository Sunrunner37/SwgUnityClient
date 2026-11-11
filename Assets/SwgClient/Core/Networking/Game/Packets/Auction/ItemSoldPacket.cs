using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Auction
{
    // Core3/MMOCoreORB/src/server/zone/packets/auction/ItemSoldMessage.h:29 (ItemSoldMessage).
    public readonly struct ItemSoldPacket : IPacket
    {
        public const uint Opcode = 0xe61cc92;
        public readonly ushort OperandCount;
        public readonly ulong SwgId;
        public readonly uint Result;
        ItemSoldPacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 3))
                throw new InvalidDataException("Unexpected ItemSoldPacket header");
            SwgId = reader.U64();
            Result = reader.U32();
            PacketFieldCodec.End(reader);
        }

        public static ItemSoldPacket ReadFrom(ByteReader reader) => new ItemSoldPacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write(SwgId);
            writer.Write(Result);
        }
    }
}
