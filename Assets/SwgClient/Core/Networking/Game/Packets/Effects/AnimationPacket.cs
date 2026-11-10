using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Effects
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/Animation.h:10 (Animation).
    public readonly struct AnimationPacket : IPacket
    {
        public const uint ControllerType = 0xf2;
        public readonly uint Flags;
        public readonly ulong ReceiverId;
        public readonly uint EnvelopeReserved;
        public readonly string Animation;
        AnimationPacket(ObjectControllerPacket envelope)
        {
            this = default;
            if (envelope.ControllerType != ControllerType)
                throw new InvalidDataException("Unexpected AnimationPacket controller type");
            Flags = envelope.Flags;
            ReceiverId = envelope.ObjectId;
            EnvelopeReserved = envelope.Reserved;
            var reader = new ByteReader(envelope.Payload);
            Animation = reader.Ascii();
            PacketFieldCodec.End(reader);
        }

        public static AnimationPacket ReadFrom(ObjectControllerPacket packet) => new AnimationPacket(packet);
        public static AnimationPacket ReadFrom(ByteReader reader) => ReadFrom(ObjectControllerPacket.ReadFrom(reader));
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(ObjectControllerPacket.OperandCount);
            writer.Write(ObjectControllerPacket.Opcode);
            writer.Write(Flags);
            writer.Write(ControllerType);
            writer.Write(ReceiverId);
            writer.Write(EnvelopeReserved);
            PacketFieldCodec.Ascii(writer, Animation);
        }
    }
}
