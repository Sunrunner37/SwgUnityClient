using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Session
{
    // Core3/MMOCoreORB/src/server/zone/packets/zone/ParametersMessage.h:12 (ParametersMessage).
    public readonly struct ParametersPacket : IPacket
    {
        public const uint Opcode = 0x487652da;
        public readonly ushort OperandCount;
        public readonly uint Value;
        ParametersPacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 2))
                throw new InvalidDataException("Unexpected ParametersPacket header");
            Value = reader.U32();
            PacketFieldCodec.End(reader);
        }

        public static ParametersPacket ReadFrom(ByteReader reader) => new ParametersPacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write(Value);
        }
    }
}
