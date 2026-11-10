using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Trade
{
    // Core3/MMOCoreORB/src/server/zone/packets/trade/UnAcceptTransactionMessage.h:8 (UnAcceptTransactionMessage).
    public readonly struct UnAcceptTransactionPacket : IPacket
    {
        public const uint Opcode = 0xe81e4382;
        public readonly ushort OperandCount;
        UnAcceptTransactionPacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 1))
                throw new InvalidDataException("Unexpected UnAcceptTransactionPacket header");
            PacketFieldCodec.End(reader);
        }

        public static UnAcceptTransactionPacket ReadFrom(ByteReader reader) => new UnAcceptTransactionPacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
        }
    }
}
