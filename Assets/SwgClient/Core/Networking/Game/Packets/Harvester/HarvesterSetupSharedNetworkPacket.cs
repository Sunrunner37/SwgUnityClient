using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Harvester
{
    // Core3/MMOCoreORB/src/server/zone/packets/installation/HarvesterSetupSharedNetworkMessage.h:14 (HarvesterSetupSharedNetworkMessage).
    public readonly struct HarvesterSetupSharedNetworkPacket : IPacket
    {
        public const uint ControllerType = 0x22b;
        public readonly uint Flags;
        public readonly ulong ReceiverId;
        public readonly uint EnvelopeReserved;
        HarvesterSetupSharedNetworkPacket(ObjectControllerPacket envelope)
        {
            this = default;
            if (envelope.ControllerType != ControllerType)
                throw new InvalidDataException("Unexpected HarvesterSetupSharedNetworkPacket controller type");
            Flags = envelope.Flags;
            ReceiverId = envelope.ObjectId;
            EnvelopeReserved = envelope.Reserved;
            var reader = new ByteReader(envelope.Payload);
            PacketFieldCodec.End(reader);
        }

        public static HarvesterSetupSharedNetworkPacket ReadFrom(ObjectControllerPacket packet) => new HarvesterSetupSharedNetworkPacket(packet);
        public static HarvesterSetupSharedNetworkPacket ReadFrom(ByteReader reader) => ReadFrom(ObjectControllerPacket.ReadFrom(reader));
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(ObjectControllerPacket.OperandCount);
            writer.Write(ObjectControllerPacket.Opcode);
            writer.Write(Flags);
            writer.Write(ControllerType);
            writer.Write(ReceiverId);
            writer.Write(EnvelopeReserved);
        }
    }
}
