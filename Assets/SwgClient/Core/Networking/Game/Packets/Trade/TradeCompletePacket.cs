using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Trade
{
    // Core3/MMOCoreORB/src/server/zone/packets/trade/TradeCompleteMessage.h:8 (TradeCompleteMessage).
    public readonly struct TradeCompletePacket : IPacket
    {
        public const uint Opcode = 0xc542038b;
        public readonly ushort OperandCount;
        TradeCompletePacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 1))
                throw new InvalidDataException("Unexpected TradeCompletePacket header");
            PacketFieldCodec.End(reader);
        }

        public static TradeCompletePacket ReadFrom(ByteReader reader) => new TradeCompletePacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
        }
    }
}
