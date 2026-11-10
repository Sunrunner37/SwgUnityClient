using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Support
{
    // Core3/MMOCoreORB/src/server/zone/packets/ui/VerifyPlayerNameResponseMessage.h:11 (VerifyPlayerNameResponseMessage).
    public readonly struct VerifyPlayerNameResponsePacket : IPacket
    {
        public const uint Opcode = 0xf4c498fd;
        public readonly ushort OperandCount;
        public readonly byte Success;
        public readonly uint Reserved;
        VerifyPlayerNameResponsePacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 9))
                throw new InvalidDataException("Unexpected VerifyPlayerNameResponsePacket header");
            Success = reader.Byte();
            Reserved = reader.U32();
            PacketFieldCodec.End(reader);
        }

        public static VerifyPlayerNameResponsePacket ReadFrom(ByteReader reader) => new VerifyPlayerNameResponsePacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write(Success);
            writer.Write(Reserved);
        }
    }
}
