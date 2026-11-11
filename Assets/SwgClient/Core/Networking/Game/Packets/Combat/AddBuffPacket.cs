using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Combat
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/Buffs.h:10 (AddBuffMessage).
    public readonly struct AddBuffPacket : IPacket
    {
        public const uint ControllerType = 0x229;
        public readonly uint Flags;
        public readonly ulong ReceiverSwgId;
        public readonly uint EnvelopeReserved;
        public readonly uint BuffCrc;
        public readonly float Duration;
        AddBuffPacket(ObjectControllerPacket envelope)
        {
            this = default;
            if (envelope.ControllerType != ControllerType)
                throw new InvalidDataException("Unexpected AddBuffPacket controller type");
            Flags = envelope.Flags;
            ReceiverSwgId = envelope.SwgId;
            EnvelopeReserved = envelope.Reserved;
            var reader = new ByteReader(envelope.Payload);
            BuffCrc = reader.U32();
            Duration = reader.Float();
            PacketFieldCodec.End(reader);
        }

        public static AddBuffPacket ReadFrom(ObjectControllerPacket packet) => new AddBuffPacket(packet);
        public static AddBuffPacket ReadFrom(ByteReader reader) => ReadFrom(ObjectControllerPacket.ReadFrom(reader));
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(ObjectControllerPacket.OperandCount);
            writer.Write(ObjectControllerPacket.Opcode);
            writer.Write(Flags);
            writer.Write(ControllerType);
            writer.Write(ReceiverSwgId);
            writer.Write(EnvelopeReserved);
            writer.Write(BuffCrc);
            writer.Write(Duration);
        }
    }
}
