using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Trade
{
    // Core3/MMOCoreORB/src/server/zone/packets/trade/AcceptTransactionMessage.h:8 (AcceptTransactionMessage).
    public readonly struct AcceptTransactionPacket : IPacket
    {
        public const uint Opcode = 0xb131ca17;
        public readonly ushort OperandCount;
        AcceptTransactionPacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 1))
                throw new InvalidDataException("Unexpected AcceptTransactionPacket header");
            PacketFieldCodec.End(reader);
        }

        public static AcceptTransactionPacket ReadFrom(ByteReader reader) => new AcceptTransactionPacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
        }
    }
}
