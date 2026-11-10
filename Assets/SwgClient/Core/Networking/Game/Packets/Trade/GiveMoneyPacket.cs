using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Trade
{
    // Core3/MMOCoreORB/src/server/zone/packets/trade/GiveMoneyMessage.h:8 (GiveMoneyMessage).
    public readonly struct GiveMoneyPacket : IPacket
    {
        public const uint Opcode = 0xd1527ee8;
        public readonly ushort OperandCount;
        public readonly uint Amount;
        GiveMoneyPacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 2))
                throw new InvalidDataException("Unexpected GiveMoneyPacket header");
            Amount = reader.U32();
            PacketFieldCodec.End(reader);
        }

        public static GiveMoneyPacket ReadFrom(ByteReader reader) => new GiveMoneyPacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write(Amount);
        }
    }
}
