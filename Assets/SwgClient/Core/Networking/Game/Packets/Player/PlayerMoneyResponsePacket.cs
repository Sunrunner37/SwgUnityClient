using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Player
{
    // Core3/MMOCoreORB/src/server/zone/packets/player/PlayerMoneyResponseMessage.h:11 (PlayerMoneyResponseMessage).
    public readonly struct PlayerMoneyResponsePacket : IPacket
    {
        public const uint Opcode = 0x367e737e;
        public readonly ushort OperandCount;
        public readonly int BankCredits;
        public readonly int CashCredits;
        PlayerMoneyResponsePacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 3))
                throw new InvalidDataException("Unexpected PlayerMoneyResponsePacket header");
            BankCredits = unchecked((int)reader.U32());
            CashCredits = unchecked((int)reader.U32());
            PacketFieldCodec.End(reader);
        }

        public static PlayerMoneyResponsePacket ReadFrom(ByteReader reader) => new PlayerMoneyResponsePacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write(BankCredits);
            writer.Write(CashCredits);
        }
    }
}
