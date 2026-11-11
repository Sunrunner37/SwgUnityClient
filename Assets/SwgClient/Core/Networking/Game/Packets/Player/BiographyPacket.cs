using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Player
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/Biography.h:11 (Biography).
    public readonly struct BiographyPacket : IPacket
    {
        public const uint ControllerType = 0x1db;
        public readonly uint Flags;
        public readonly ulong ReceiverSwgId;
        public readonly uint EnvelopeReserved;
        public readonly ulong PlayerSwgId;
        public readonly string Biography;
        BiographyPacket(ObjectControllerPacket envelope)
        {
            this = default;
            if (envelope.ControllerType != ControllerType)
                throw new InvalidDataException("Unexpected BiographyPacket controller type");
            Flags = envelope.Flags;
            ReceiverSwgId = envelope.SwgId;
            EnvelopeReserved = envelope.Reserved;
            var reader = new ByteReader(envelope.Payload);
            PlayerSwgId = reader.U64();
            Biography = reader.Unicode();
            PacketFieldCodec.End(reader);
        }

        public static BiographyPacket ReadFrom(ObjectControllerPacket packet) => new BiographyPacket(packet);
        public static BiographyPacket ReadFrom(ByteReader reader) => ReadFrom(ObjectControllerPacket.ReadFrom(reader));
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(ObjectControllerPacket.OperandCount);
            writer.Write(ObjectControllerPacket.Opcode);
            writer.Write(Flags);
            writer.Write(ControllerType);
            writer.Write(ReceiverSwgId);
            writer.Write(EnvelopeReserved);
            writer.Write(PlayerSwgId);
            PacketFieldCodec.Unicode(writer, Biography);
        }
    }
}
