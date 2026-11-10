using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Chat
{
    // Core3/MMOCoreORB/src/server/zone/packets/chat/ChatOnCreateRoom.h:11 (ChatOnCreateRoom).
    public readonly struct ChatOnCreateRoomPacket : IPacket
    {
        public const uint Opcode = 0x35d7cc9f;
        public readonly ushort OperandCount;
        public readonly uint Error;
        public readonly ChatRoomData Room;
        public readonly uint RequestId;
        ChatOnCreateRoomPacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 4))
                throw new InvalidDataException("Unexpected ChatOnCreateRoomPacket header");
            Error = reader.U32();
            Room = ChatRoomData.ReadFrom(reader);
            RequestId = reader.U32();
            PacketFieldCodec.End(reader);
        }

        public static ChatOnCreateRoomPacket ReadFrom(ByteReader reader) => new ChatOnCreateRoomPacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write(Error);
            Room.WriteTo(writer);
            writer.Write(RequestId);
        }
    }
}
