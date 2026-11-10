using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Conversation
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/NpcConversationMessage.h:14 (NpcConversationMessage).
    public readonly struct NpcConversationPacket : IPacket
    {
        public const uint ControllerType = 0xdf;
        public readonly uint Flags;
        public readonly ulong ReceiverId;
        public readonly uint EnvelopeReserved;
        public readonly ConversationText Message;
        NpcConversationPacket(ObjectControllerPacket envelope)
        {
            this = default;
            if (envelope.ControllerType != ControllerType)
                throw new InvalidDataException("Unexpected NpcConversationPacket controller type");
            Flags = envelope.Flags;
            ReceiverId = envelope.ObjectId;
            EnvelopeReserved = envelope.Reserved;
            var reader = new ByteReader(envelope.Payload);
            Message = ConversationText.ReadFrom(reader);
            PacketFieldCodec.End(reader);
        }

        public static NpcConversationPacket ReadFrom(ObjectControllerPacket packet) => new NpcConversationPacket(packet);
        public static NpcConversationPacket ReadFrom(ByteReader reader) => ReadFrom(ObjectControllerPacket.ReadFrom(reader));
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(ObjectControllerPacket.OperandCount);
            writer.Write(ObjectControllerPacket.Opcode);
            writer.Write(Flags);
            writer.Write(ControllerType);
            writer.Write(ReceiverId);
            writer.Write(EnvelopeReserved);
            Message.WriteTo(writer);
        }
    }
}
