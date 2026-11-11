using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Harvester
{
    // Core3/MMOCoreORB/src/server/zone/packets/harvester/HarvesterResourceDataMessage.h:10 (HarvesterResourceDataMessage).
    public readonly struct HarvesterResourceDataPacket : IPacket
    {
        public const uint ControllerType = 0xea;
        public readonly uint Flags;
        public readonly ulong ReceiverSwgId;
        public readonly uint EnvelopeReserved;
        public readonly ulong HarvesterSwgId;
        public readonly HarvesterResource[] Resources;
        HarvesterResourceDataPacket(ObjectControllerPacket envelope)
        {
            this = default;
            if (envelope.ControllerType != ControllerType)
                throw new InvalidDataException("Unexpected HarvesterResourceDataPacket controller type");
            Flags = envelope.Flags;
            ReceiverSwgId = envelope.SwgId;
            EnvelopeReserved = envelope.Reserved;
            var reader = new ByteReader(envelope.Payload);
            HarvesterSwgId = reader.U64();
            Resources = new HarvesterResource[PacketFieldCodec.Count(reader, reader.U32(), 13)];
            for (int i = 0; i < Resources.Length; i++)
                Resources[i] = HarvesterResource.ReadFrom(reader);
            PacketFieldCodec.End(reader);
        }

        public static HarvesterResourceDataPacket ReadFrom(ObjectControllerPacket packet) => new HarvesterResourceDataPacket(packet);
        public static HarvesterResourceDataPacket ReadFrom(ByteReader reader) => ReadFrom(ObjectControllerPacket.ReadFrom(reader));
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(ObjectControllerPacket.OperandCount);
            writer.Write(ObjectControllerPacket.Opcode);
            writer.Write(Flags);
            writer.Write(ControllerType);
            writer.Write(ReceiverSwgId);
            writer.Write(EnvelopeReserved);
            writer.Write(HarvesterSwgId);
            writer.Write(checked((uint)Resources.Length));
            foreach (var item in Resources)
                item.WriteTo(writer);
        }
    }
}
