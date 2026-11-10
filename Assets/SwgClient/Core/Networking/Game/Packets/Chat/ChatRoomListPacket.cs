using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Chat
{
    // Core3/MMOCoreORB/src/server/zone/packets/chat/ChatRoomList.h:17 (ChatRoomList).
    public readonly struct ChatRoomListPacket : IPacket
    {
        public const uint Opcode = 0x70deb197;
        public readonly ushort OperandCount;
        public readonly ChatRoomData[] Rooms;
        ChatRoomListPacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 2))
                throw new InvalidDataException("Unexpected ChatRoomListPacket header");
            Rooms = new ChatRoomData[PacketFieldCodec.Count(reader, reader.U32(), 35)];
            for (int i = 0; i < Rooms.Length; i++)
                Rooms[i] = ChatRoomData.ReadFrom(reader);
            PacketFieldCodec.End(reader);
        }

        public static ChatRoomListPacket ReadFrom(ByteReader reader) => new ChatRoomListPacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write(checked((uint)Rooms.Length));
            foreach (var item in Rooms)
                item.WriteTo(writer);
        }
    }
}
