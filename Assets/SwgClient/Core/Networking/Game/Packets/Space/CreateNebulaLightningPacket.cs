using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Space
{
    // Core3/MMOCoreORB/src/server/zone/packets/jtl/CreateNebulaLightningMessage.h:13 (CreateNebulaLightningMessage).
    public readonly struct CreateNebulaLightningPacket : IPacket
    {
        public const uint Opcode = 0x65f27987;
        public readonly ushort OperandCount;
        public readonly ushort Reserved;
        public readonly uint[] ReservedInts;
        CreateNebulaLightningPacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 17))
                throw new InvalidDataException("Unexpected CreateNebulaLightningPacket header");
            Reserved = reader.U16();
            ReservedInts = new uint[9];
            for (int i = 0; i < ReservedInts.Length; i++)
                ReservedInts[i] = reader.U32();
            PacketFieldCodec.End(reader);
        }

        public static CreateNebulaLightningPacket ReadFrom(ByteReader reader) => new CreateNebulaLightningPacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write(Reserved);
            foreach (var item in ReservedInts)
                writer.Write(item);
        }
    }
}
