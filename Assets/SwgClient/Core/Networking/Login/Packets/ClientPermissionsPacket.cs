using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Networking.Login.Packets
{
    public readonly struct ClientPermissionsPacket : IPacket
    {
        public const ushort OperandCount = 4;
        public const uint Opcode = 0xe00730e5;
        public readonly bool CanLogin;
        public readonly bool CanCreate;
        public readonly byte Reserved;
        public ClientPermissionsPacket(bool canLogin, bool canCreate, byte reserved = 0)
        {
            CanLogin = canLogin;
            CanCreate = canCreate;
            Reserved = reserved;
        }

        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write((byte)(CanLogin ? 1 : 0));
            writer.Write((byte)(CanCreate ? 1 : 0));
            writer.Write(Reserved);
        }

        public static ClientPermissionsPacket ReadFrom(ByteReader reader)
        {
            if (reader.U16() != OperandCount || reader.U32() != Opcode)
                throw new InvalidDataException("Unexpected login packet header");
            return new ClientPermissionsPacket(reader.Byte() != 0, reader.Byte() != 0, reader.Byte());
        }
    }
}
