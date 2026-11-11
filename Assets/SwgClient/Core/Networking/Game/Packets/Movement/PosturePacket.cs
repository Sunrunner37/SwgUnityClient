using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Movement
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/PostureMessage.h:10 (PostureMessage).
    public readonly struct PosturePacket : IPacket
    {
        public const uint ControllerType = 0x131;
        public readonly uint Flags;
        public readonly ulong ReceiverSwgId;
        public readonly uint EnvelopeReserved;
        public readonly byte Posture;
        public readonly byte Reserved;
        PosturePacket(ObjectControllerPacket envelope)
        {
            this = default;
            if (envelope.ControllerType != ControllerType)
                throw new InvalidDataException("Unexpected PosturePacket controller type");
            Flags = envelope.Flags;
            ReceiverSwgId = envelope.SwgId;
            EnvelopeReserved = envelope.Reserved;
            var reader = new ByteReader(envelope.Payload);
            Posture = reader.Byte();
            Reserved = reader.Byte();
            PacketFieldCodec.End(reader);
        }

        public static PosturePacket ReadFrom(ObjectControllerPacket packet) => new PosturePacket(packet);
        public static PosturePacket ReadFrom(ByteReader reader) => ReadFrom(ObjectControllerPacket.ReadFrom(reader));
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(ObjectControllerPacket.OperandCount);
            writer.Write(ObjectControllerPacket.Opcode);
            writer.Write(Flags);
            writer.Write(ControllerType);
            writer.Write(ReceiverSwgId);
            writer.Write(EnvelopeReserved);
            writer.Write(Posture);
            writer.Write(Reserved);
        }
    }
}
