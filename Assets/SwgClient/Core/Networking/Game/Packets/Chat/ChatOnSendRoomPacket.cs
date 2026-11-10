using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Chat
{
    // Core3/MMOCoreORB/src/server/zone/packets/chat/ChatOnSendRoomMessage.h:10 (ChatOnSendRoomMessage).
    public readonly struct ChatOnSendRoomPacket : IPacket
    {
        public const uint Opcode = 0xe7b61633;
        public readonly ushort OperandCount;
        public readonly uint Result;
        public readonly uint Sequence;
        ChatOnSendRoomPacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 3))
                throw new InvalidDataException("Unexpected ChatOnSendRoomPacket header");
            Result = reader.U32();
            Sequence = reader.U32();
            PacketFieldCodec.End(reader);
        }

        public static ChatOnSendRoomPacket ReadFrom(ByteReader reader) => new ChatOnSendRoomPacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write(Result);
            writer.Write(Sequence);
        }
    }
}
