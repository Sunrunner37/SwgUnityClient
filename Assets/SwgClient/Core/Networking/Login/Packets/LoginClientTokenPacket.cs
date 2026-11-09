using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Networking.Login.Packets
{
    public readonly struct LoginClientTokenPacket : IPacket
    {
        public const ushort OperandCount = 4;
        public const uint Opcode = 0xaab296c6;
        public const int MaximumTokenBlockLength = 4096;
        public readonly byte[] Token;
        public readonly uint AccountId;
        public readonly uint StationId;
        public readonly string Username;
        public LoginClientTokenPacket(byte[] token, uint accountId, uint stationId, string username)
        {
            ValidateToken(token);
            Token = token;
            AccountId = accountId;
            StationId = stationId;
            Username = username;
        }

        internal static void ValidateToken(byte[] token)
        {
            if (token == null || token.Length == 0 || token.Length > MaximumTokenBlockLength - sizeof(uint))
                throw new InvalidDataException("Invalid account token length");
        }

        internal static byte[] ReadToken(ByteReader reader)
        {
            int length = checked((int)reader.U32());
            if (length < sizeof(uint) + 1 || length > MaximumTokenBlockLength)
                throw new InvalidDataException("Invalid account token length");
            return reader.Take(length - sizeof(uint));
        }

        public void WriteTo(BinaryWriter writer)
        {
            ValidateToken(Token);
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write(checked((uint)Token.Length + sizeof(uint)));
            writer.Write(Token);
            writer.Write(AccountId);
            writer.Write(StationId);
            Wire.Ascii(writer, Username);
        }

        public static LoginClientTokenPacket ReadFrom(ByteReader reader)
        {
            if (reader.U16() != OperandCount || reader.U32() != Opcode)
                throw new InvalidDataException("Unexpected login packet header");
            var token = ReadToken(reader);
            uint accountId = reader.U32();
            uint stationId = reader.U32();
            return new LoginClientTokenPacket(token, accountId, stationId, reader.Ascii());
        }
    }
}
