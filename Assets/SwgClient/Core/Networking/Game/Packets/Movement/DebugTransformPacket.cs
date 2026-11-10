using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Movement
{
    // Core3/MMOCoreORB/src/server/zone/packets/scene/DebugTransformMessage.h:13 (DebugTransformMessage).
    public readonly struct DebugTransformPacket : IPacket
    {
        public const uint Opcode = 0xd0cdaa62;
        public readonly ushort OperandCount;
        public readonly uint[] ReservedInts;
        public readonly ulong ReservedId;
        DebugTransformPacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 9))
                throw new InvalidDataException("Unexpected DebugTransformPacket header");
            ReservedInts = new uint[7];
            for (int i = 0; i < ReservedInts.Length; i++)
                ReservedInts[i] = reader.U32();
            ReservedId = reader.U64();
            PacketFieldCodec.End(reader);
        }

        public static DebugTransformPacket ReadFrom(ByteReader reader) => new DebugTransformPacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            foreach (var item in ReservedInts)
                writer.Write(item);
            writer.Write(ReservedId);
        }
    }
}
