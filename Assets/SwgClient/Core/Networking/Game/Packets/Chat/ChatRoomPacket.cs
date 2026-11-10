using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Chat
{
    // Core3/MMOCoreORB/src/server/zone/packets/chat/ChatRoomMessage.h:10 (ChatRoomMessage).
    public readonly struct ChatRoomPacket : IPacket
    {
        public const uint Opcode = 0xcd4ce444;
        public readonly ushort OperandCount;
        public readonly ChatAvatar Sender;
        public readonly uint ChannelId;
        public readonly string Message;
        public readonly uint Reserved;
        ChatRoomPacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 5))
                throw new InvalidDataException("Unexpected ChatRoomPacket header");
            Sender = ChatAvatar.ReadFrom(reader);
            ChannelId = reader.U32();
            Message = reader.Unicode();
            Reserved = reader.U32();
            PacketFieldCodec.End(reader);
        }

        public static ChatRoomPacket ReadFrom(ByteReader reader) => new ChatRoomPacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            Sender.WriteTo(writer);
            writer.Write(ChannelId);
            PacketFieldCodec.Unicode(writer, Message);
            writer.Write(Reserved);
        }
    }
}
