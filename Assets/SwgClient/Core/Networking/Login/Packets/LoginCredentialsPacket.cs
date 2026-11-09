using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Networking.Login.Packets
{
    public readonly struct LoginCredentialsPacket : IPacket
    {
        public const ushort OperandCount = 4;
        public const uint Opcode = 0x41131f96;
        public const int MaximumLength = 450;
        public readonly string Username;
        public readonly string Password;
        public readonly string ClientVersion;
        public LoginCredentialsPacket(string username, string password, string clientVersion)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
                throw new InvalidDataException("Enter your username and password");
            Wire.ValidateAscii(username);
            Wire.ValidateAscii(password);
            Wire.ValidateAscii(clientVersion);
            if (Wire.HeaderSize + 3 * sizeof(ushort) + username.Length + password.Length + clientVersion.Length > MaximumLength)
                throw new InvalidDataException("Login credentials too long");
            Username = username;
            Password = password;
            ClientVersion = clientVersion;
        }

        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            Wire.Ascii(writer, Username);
            Wire.Ascii(writer, Password);
            Wire.Ascii(writer, ClientVersion);
        }

        public static LoginCredentialsPacket ReadFrom(ByteReader reader)
        {
            if (reader.U16() != OperandCount || reader.U32() != Opcode)
                throw new InvalidDataException("Unexpected login packet header");
            return new LoginCredentialsPacket(reader.Ascii(), reader.Ascii(), reader.Ascii());
        }
    }
}
