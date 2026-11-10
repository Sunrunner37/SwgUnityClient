using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Mission
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/MissionAbortCallback.h:56 (MissionAbortResponse).
    public readonly struct MissionAbortResponsePacket : IPacket
    {
        public const uint ControllerType = 0x142;
        public readonly uint Flags;
        public readonly ulong ReceiverId;
        public readonly uint EnvelopeReserved;
        public readonly ulong MissionId;
        MissionAbortResponsePacket(ObjectControllerPacket envelope)
        {
            this = default;
            if (envelope.ControllerType != ControllerType)
                throw new InvalidDataException("Unexpected MissionAbortResponsePacket controller type");
            Flags = envelope.Flags;
            ReceiverId = envelope.ObjectId;
            EnvelopeReserved = envelope.Reserved;
            var reader = new ByteReader(envelope.Payload);
            MissionId = reader.U64();
            PacketFieldCodec.End(reader);
        }

        public static MissionAbortResponsePacket ReadFrom(ObjectControllerPacket packet) => new MissionAbortResponsePacket(packet);
        public static MissionAbortResponsePacket ReadFrom(ByteReader reader) => ReadFrom(ObjectControllerPacket.ReadFrom(reader));
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(ObjectControllerPacket.OperandCount);
            writer.Write(ObjectControllerPacket.Opcode);
            writer.Write(Flags);
            writer.Write(ControllerType);
            writer.Write(ReceiverId);
            writer.Write(EnvelopeReserved);
            writer.Write(MissionId);
        }
    }
}
