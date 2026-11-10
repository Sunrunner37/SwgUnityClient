using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Space
{
    // Core3/MMOCoreORB/src/server/zone/packets/jtl/CreateMissileMessage.h:13 (CreateMissileMessage).
    public readonly struct CreateMissilePacket : IPacket
    {
        public const uint Opcode = 0x721cf08b;
        public readonly ushort OperandCount;
        public readonly uint Reserved1;
        public readonly ulong SourceId;
        public readonly ulong TargetId;
        public readonly uint[] ReservedInts;
        CreateMissilePacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 20))
                throw new InvalidDataException("Unexpected CreateMissilePacket header");
            Reserved1 = reader.U32();
            SourceId = reader.U64();
            TargetId = reader.U64();
            ReservedInts = new uint[10];
            for (int i = 0; i < ReservedInts.Length; i++)
                ReservedInts[i] = reader.U32();
            PacketFieldCodec.End(reader);
        }

        public static CreateMissilePacket ReadFrom(ByteReader reader) => new CreateMissilePacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write(Reserved1);
            writer.Write(SourceId);
            writer.Write(TargetId);
            foreach (var item in ReservedInts)
                writer.Write(item);
        }
    }
}
