using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Trade
{
    // Core3/MMOCoreORB/src/server/zone/packets/trade/AbortTradeMessage.h:8 (AbortTradeMessage).
    public readonly struct AbortTradePacket : IPacket
    {
        public const uint Opcode = 0x9ca80f98;
        public readonly ushort OperandCount;
        AbortTradePacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 1))
                throw new InvalidDataException("Unexpected AbortTradePacket header");
            PacketFieldCodec.End(reader);
        }

        public static AbortTradePacket ReadFrom(ByteReader reader) => new AbortTradePacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
        }
    }
}
