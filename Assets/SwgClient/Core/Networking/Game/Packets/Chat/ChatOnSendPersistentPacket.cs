using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Chat
{
    // Core3/MMOCoreORB/src/server/zone/packets/chat/ChatOnSendPersistentMessage.h:12 (ChatOnSendPersistentMessage).
    public readonly struct ChatOnSendPersistentPacket : IPacket
    {
        public const uint Opcode = 0x94e7a7ae;
        public readonly ushort OperandCount;
        public readonly uint Result;
        public readonly uint Sequence;
        ChatOnSendPersistentPacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 3))
                throw new InvalidDataException("Unexpected ChatOnSendPersistentPacket header");
            Result = reader.U32();
            Sequence = reader.U32();
            PacketFieldCodec.End(reader);
        }

        public static ChatOnSendPersistentPacket ReadFrom(ByteReader reader) => new ChatOnSendPersistentPacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write(Result);
            writer.Write(Sequence);
        }
    }
}
