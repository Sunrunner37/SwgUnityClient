using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Scene
{
    // Core3/MMOCoreORB/src/server/zone/packets/scene/IsFlattenedTheaterMessage.h:13 (IsFlattenedTheaterMessage).
    public readonly struct IsFlattenedTheaterPacket : IPacket
    {
        public const uint Opcode = 0x4eb0b06a;
        public readonly ushort OperandCount;
        public readonly ulong SwgId;
        public readonly byte ShouldFlatten;
        IsFlattenedTheaterPacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 3))
                throw new InvalidDataException("Unexpected IsFlattenedTheaterPacket header");
            SwgId = reader.U64();
            ShouldFlatten = reader.Byte();
            PacketFieldCodec.End(reader);
        }

        public static IsFlattenedTheaterPacket ReadFrom(ByteReader reader) => new IsFlattenedTheaterPacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write(SwgId);
            writer.Write(ShouldFlatten);
        }
    }
}
