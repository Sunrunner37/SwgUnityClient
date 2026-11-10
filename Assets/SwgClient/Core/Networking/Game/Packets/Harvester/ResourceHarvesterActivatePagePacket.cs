using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Harvester
{
    // Core3/MMOCoreORB/src/server/zone/packets/harvester/ResourceHarvesterActivatePageMessage.h:11 (ResourceHarvesterActivatePageMessage).
    public readonly struct ResourceHarvesterActivatePagePacket : IPacket
    {
        public const uint Opcode = 0xbd18c679;
        public readonly ushort OperandCount;
        public readonly ulong ObjectId;
        ResourceHarvesterActivatePagePacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 2))
                throw new InvalidDataException("Unexpected ResourceHarvesterActivatePagePacket header");
            ObjectId = reader.U64();
            PacketFieldCodec.End(reader);
        }

        public static ResourceHarvesterActivatePagePacket ReadFrom(ByteReader reader) => new ResourceHarvesterActivatePagePacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write(ObjectId);
        }
    }
}
