using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Combat
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/Buffs.h:27 (RemoveBuffMessage).
    public readonly struct RemoveBuffPacket : IPacket
    {
        public const uint ControllerType = 0x22a;
        public readonly uint Flags;
        public readonly ulong ReceiverSwgId;
        public readonly uint EnvelopeReserved;
        public readonly uint BuffCrc;
        RemoveBuffPacket(ObjectControllerPacket envelope)
        {
            this = default;
            if (envelope.ControllerType != ControllerType)
                throw new InvalidDataException("Unexpected RemoveBuffPacket controller type");
            Flags = envelope.Flags;
            ReceiverSwgId = envelope.SwgId;
            EnvelopeReserved = envelope.Reserved;
            var reader = new ByteReader(envelope.Payload);
            BuffCrc = reader.U32();
            PacketFieldCodec.End(reader);
        }

        public static RemoveBuffPacket ReadFrom(ObjectControllerPacket packet) => new RemoveBuffPacket(packet);
        public static RemoveBuffPacket ReadFrom(ByteReader reader) => ReadFrom(ObjectControllerPacket.ReadFrom(reader));
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(ObjectControllerPacket.OperandCount);
            writer.Write(ObjectControllerPacket.Opcode);
            writer.Write(Flags);
            writer.Write(ControllerType);
            writer.Write(ReceiverSwgId);
            writer.Write(EnvelopeReserved);
            writer.Write(BuffCrc);
        }
    }
}
