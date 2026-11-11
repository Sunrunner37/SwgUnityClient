using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Space
{
    // Core3/MMOCoreORB/src/server/zone/packets/jtl/DogfightTauntPlayerMessage.h:13 (DogfightTauntPlayerMessage).
    public readonly struct DogfightTauntPlayerPacket : IPacket
    {
        public const uint Opcode = 0xca2a548b;
        public readonly ushort OperandCount;
        public readonly ulong SwgId;
        public readonly uint Reserved;
        DogfightTauntPlayerPacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 9))
                throw new InvalidDataException("Unexpected DogfightTauntPlayerPacket header");
            SwgId = reader.U64();
            Reserved = reader.U32();
            PacketFieldCodec.End(reader);
        }

        public static DogfightTauntPlayerPacket ReadFrom(ByteReader reader) => new DogfightTauntPlayerPacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write(SwgId);
            writer.Write(Reserved);
        }
    }
}
