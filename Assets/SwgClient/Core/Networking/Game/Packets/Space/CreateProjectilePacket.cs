using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Space
{
    // Core3/MMOCoreORB/src/server/zone/packets/jtl/CreateProjectileMessage.h:13 (CreateProjectileMessage).
    public readonly struct CreateProjectilePacket : IPacket
    {
        public const uint Opcode = 0xb88af9a5;
        public readonly ushort OperandCount;
        public readonly ushort Reserved1;
        public readonly byte[] ReservedBytes;
        public readonly ushort[] ReservedShorts;
        public readonly uint Reserved2;
        CreateProjectilePacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 18))
                throw new InvalidDataException("Unexpected CreateProjectilePacket header");
            Reserved1 = reader.U16();
            ReservedBytes = new byte[3];
            for (int i = 0; i < ReservedBytes.Length; i++)
                ReservedBytes[i] = reader.Byte();
            ReservedShorts = new ushort[6];
            for (int i = 0; i < ReservedShorts.Length; i++)
                ReservedShorts[i] = reader.U16();
            Reserved2 = reader.U32();
            PacketFieldCodec.End(reader);
        }

        public static CreateProjectilePacket ReadFrom(ByteReader reader) => new CreateProjectilePacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write(Reserved1);
            foreach (var item in ReservedBytes)
                writer.Write(item);
            foreach (var item in ReservedShorts)
                writer.Write(item);
            writer.Write(Reserved2);
        }
    }
}
