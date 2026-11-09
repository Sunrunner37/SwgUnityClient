using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Networking.Login.Packets
{
    public readonly struct ClientIdentificationPacket : IPacket
    {
        public const ushort OperandCount = 3;
        public const uint Opcode = 0xd5899226;
        public readonly uint GameBits;
        public readonly byte[] Token;
        public readonly uint AccountId;
        public readonly string ClientVersion;
        public ClientIdentificationPacket(byte[] token, uint accountId, string clientVersion, uint gameBits = 0)
        {
            LoginClientTokenPacket.ValidateToken(token);
            Wire.ValidateAscii(clientVersion);
            Token = token;
            AccountId = accountId;
            ClientVersion = clientVersion;
            GameBits = gameBits;
        }

        public void WriteTo(BinaryWriter writer)
        {
            LoginClientTokenPacket.ValidateToken(Token);
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write(GameBits);
            writer.Write(checked((uint)Token.Length + sizeof(uint))); // Account ID is embedded in the token block.
            writer.Write(Token);
            writer.Write(AccountId);
            Wire.Ascii(writer, ClientVersion);
        }

        public static ClientIdentificationPacket ReadFrom(ByteReader reader)
        {
            if (reader.U16() != OperandCount || reader.U32() != Opcode)
                throw new InvalidDataException("Unexpected login packet header");
            uint gameBits = reader.U32();
            var token = LoginClientTokenPacket.ReadToken(reader);
            uint accountId = reader.U32();
            return new ClientIdentificationPacket(token, accountId, reader.Ascii(), gameBits);
        }
    }
}
