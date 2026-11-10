using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Trade
{
    // Core3/MMOCoreORB/src/server/zone/packets/trade/DenyTradeMessage.h:11 (DenyTradeMessage).
    public readonly struct DenyTradePacket : IPacket
    {
        public const uint Opcode = 0x6ec28670;
        public readonly ushort OperandCount;
        DenyTradePacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 1))
                throw new InvalidDataException("Unexpected DenyTradePacket header");
            PacketFieldCodec.End(reader);
        }

        public static DenyTradePacket ReadFrom(ByteReader reader) => new DenyTradePacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
        }
    }
}
