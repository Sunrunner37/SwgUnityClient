using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Chat
{
    // Core3/MMOCoreORB/src/server/zone/packets/chat/ChatOnLeaveRoom.h:12 (ChatOnLeaveRoom).
    public readonly struct ChatOnLeaveRoomPacket : IPacket
    {
        public const uint Opcode = 0x60b5098b;
        public readonly ushort OperandCount;
        public readonly ChatAvatar Player;
        public readonly uint Error;
        public readonly uint RoomId;
        public readonly uint RequestId;
        ChatOnLeaveRoomPacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 3))
                throw new InvalidDataException("Unexpected ChatOnLeaveRoomPacket header");
            Player = ChatAvatar.ReadFrom(reader);
            Error = reader.U32();
            RoomId = reader.U32();
            RequestId = reader.U32();
            PacketFieldCodec.End(reader);
        }

        public static ChatOnLeaveRoomPacket ReadFrom(ByteReader reader) => new ChatOnLeaveRoomPacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            Player.WriteTo(writer);
            writer.Write(Error);
            writer.Write(RoomId);
            writer.Write(RequestId);
        }
    }
}
