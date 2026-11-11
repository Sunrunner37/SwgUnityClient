using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Conversation
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/StartNpcConversation.h:10 (StartNpcConversation).
    public readonly struct StartNpcConversationPacket : IPacket
    {
        public const uint ControllerType = 0xdd;
        public readonly uint Flags;
        public readonly ulong ReceiverSwgId;
        public readonly uint EnvelopeReserved;
        public readonly ulong NpcSwgId;
        public readonly byte Reserved1;
        public readonly string ConversationFile;
        public readonly uint Reserved2;
        StartNpcConversationPacket(ObjectControllerPacket envelope)
        {
            this = default;
            if (envelope.ControllerType != ControllerType)
                throw new InvalidDataException("Unexpected StartNpcConversationPacket controller type");
            Flags = envelope.Flags;
            ReceiverSwgId = envelope.SwgId;
            EnvelopeReserved = envelope.Reserved;
            var reader = new ByteReader(envelope.Payload);
            NpcSwgId = reader.U64();
            Reserved1 = reader.Byte();
            ConversationFile = reader.Ascii();
            Reserved2 = reader.U32();
            PacketFieldCodec.End(reader);
        }

        public static StartNpcConversationPacket ReadFrom(ObjectControllerPacket packet) => new StartNpcConversationPacket(packet);
        public static StartNpcConversationPacket ReadFrom(ByteReader reader) => ReadFrom(ObjectControllerPacket.ReadFrom(reader));
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(ObjectControllerPacket.OperandCount);
            writer.Write(ObjectControllerPacket.Opcode);
            writer.Write(Flags);
            writer.Write(ControllerType);
            writer.Write(ReceiverSwgId);
            writer.Write(EnvelopeReserved);
            writer.Write(NpcSwgId);
            writer.Write(Reserved1);
            PacketFieldCodec.Ascii(writer, ConversationFile);
            writer.Write(Reserved2);
        }
    }
}
