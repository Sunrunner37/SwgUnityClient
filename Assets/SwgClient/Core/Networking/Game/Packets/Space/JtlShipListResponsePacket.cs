using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Space
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/JtlShipListResponse.h:15 (JtlShipListResponse).
    public readonly struct JtlShipListResponsePacket : IPacket
    {
        public const uint ControllerType = 0x41d;
        public readonly uint Flags;
        public readonly ulong ReceiverId;
        public readonly uint EnvelopeReserved;
        public readonly uint ShipCount;
        JtlShipListResponsePacket(ObjectControllerPacket envelope)
        {
            this = default;
            if (envelope.ControllerType != ControllerType)
                throw new InvalidDataException("Unexpected JtlShipListResponsePacket controller type");
            Flags = envelope.Flags;
            ReceiverId = envelope.ObjectId;
            EnvelopeReserved = envelope.Reserved;
            var reader = new ByteReader(envelope.Payload);
            ShipCount = reader.U32();
            PacketFieldCodec.End(reader);
        }

        public static JtlShipListResponsePacket ReadFrom(ObjectControllerPacket packet) => new JtlShipListResponsePacket(packet);
        public static JtlShipListResponsePacket ReadFrom(ByteReader reader) => ReadFrom(ObjectControllerPacket.ReadFrom(reader));
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(ObjectControllerPacket.OperandCount);
            writer.Write(ObjectControllerPacket.Opcode);
            writer.Write(Flags);
            writer.Write(ControllerType);
            writer.Write(ReceiverId);
            writer.Write(EnvelopeReserved);
            writer.Write(ShipCount);
        }
    }
}
