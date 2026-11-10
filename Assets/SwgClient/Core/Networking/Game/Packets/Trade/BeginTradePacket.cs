using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Trade
{
    // Core3/MMOCoreORB/src/server/zone/packets/trade/BeginTradeMessage.h:8 (BeginTradeMessage).
    public readonly struct BeginTradePacket : IPacket
    {
        public const uint Opcode = 0x325932d8;
        public readonly ushort OperandCount;
        public readonly ulong PlayerId;
        BeginTradePacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 2))
                throw new InvalidDataException("Unexpected BeginTradePacket header");
            PlayerId = reader.U64();
            PacketFieldCodec.End(reader);
        }

        public static BeginTradePacket ReadFrom(ByteReader reader) => new BeginTradePacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write(PlayerId);
        }
    }
}
