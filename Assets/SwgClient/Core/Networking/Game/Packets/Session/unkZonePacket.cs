using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Session
{
    // Core3/MMOCoreORB/src/server/zone/packets/zone/unkZone.h:14 (unkZone).
    public readonly struct unkZonePacket : IPacket
    {
        public const uint Opcode = 0x58c07f21;
        public readonly ushort OperandCount;
        public readonly byte[] Reserved;
        unkZonePacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 2))
                throw new InvalidDataException("Unexpected unkZonePacket header");
            Reserved = new byte[4];
            for (int i = 0; i < Reserved.Length; i++)
                Reserved[i] = reader.Byte();
            PacketFieldCodec.End(reader);
        }

        public static unkZonePacket ReadFrom(ByteReader reader) => new unkZonePacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            foreach (var item in Reserved)
                writer.Write(item);
        }
    }
}
