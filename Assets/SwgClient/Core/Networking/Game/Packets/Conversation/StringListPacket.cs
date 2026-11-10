using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Conversation
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/StringList.h:10 (StringList).
    public readonly struct StringListPacket : IPacket
    {
        public const uint ControllerType = 0xe0;
        public readonly uint Flags;
        public readonly ulong ReceiverId;
        public readonly uint EnvelopeReserved;
        public readonly string[] Options;
        StringListPacket(ObjectControllerPacket envelope)
        {
            this = default;
            if (envelope.ControllerType != ControllerType)
                throw new InvalidDataException("Unexpected StringListPacket controller type");
            Flags = envelope.Flags;
            ReceiverId = envelope.ObjectId;
            EnvelopeReserved = envelope.Reserved;
            var reader = new ByteReader(envelope.Payload);
            Options = new string[PacketFieldCodec.Count(reader, reader.Byte(), 4)];
            for (int i = 0; i < Options.Length; i++)
                Options[i] = reader.Unicode();
            PacketFieldCodec.End(reader);
        }

        public static StringListPacket ReadFrom(ObjectControllerPacket packet) => new StringListPacket(packet);
        public static StringListPacket ReadFrom(ByteReader reader) => ReadFrom(ObjectControllerPacket.ReadFrom(reader));
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(ObjectControllerPacket.OperandCount);
            writer.Write(ObjectControllerPacket.Opcode);
            writer.Write(Flags);
            writer.Write(ControllerType);
            writer.Write(ReceiverId);
            writer.Write(EnvelopeReserved);
            writer.Write(checked((byte)Options.Length));
            foreach (var item in Options)
                PacketFieldCodec.Unicode(writer, item);
        }
    }
}
