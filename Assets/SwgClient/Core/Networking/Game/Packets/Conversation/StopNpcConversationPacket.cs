using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Conversation
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/StopNpcConversation.h:8 (StopNpcConversation).
    public readonly struct StopNpcConversationPacket : IPacket
    {
        public const uint ControllerType = 0xde;
        public readonly uint Flags;
        public readonly ulong ReceiverId;
        public readonly uint EnvelopeReserved;
        public readonly ulong NpcId;
        StopNpcConversationPacket(ObjectControllerPacket envelope)
        {
            this = default;
            if (envelope.ControllerType != ControllerType)
                throw new InvalidDataException("Unexpected StopNpcConversationPacket controller type");
            Flags = envelope.Flags;
            ReceiverId = envelope.ObjectId;
            EnvelopeReserved = envelope.Reserved;
            var reader = new ByteReader(envelope.Payload);
            NpcId = reader.U64();
            PacketFieldCodec.End(reader);
        }

        public static StopNpcConversationPacket ReadFrom(ObjectControllerPacket packet) => new StopNpcConversationPacket(packet);
        public static StopNpcConversationPacket ReadFrom(ByteReader reader) => ReadFrom(ObjectControllerPacket.ReadFrom(reader));
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(ObjectControllerPacket.OperandCount);
            writer.Write(ObjectControllerPacket.Opcode);
            writer.Write(Flags);
            writer.Write(ControllerType);
            writer.Write(ReceiverId);
            writer.Write(EnvelopeReserved);
            writer.Write(NpcId);
        }
    }
}
