using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Chat
{
    // Core3/MMOCoreORB/src/server/zone/packets/chat/ChatOnEnteredRoom.h:10 (ChatOnEnteredRoom).
    public readonly struct ChatOnEnteredRoomPacket : IPacket
    {
        public const uint Opcode = 0xe69bdc0a;
        public readonly ushort OperandCount;
        public readonly ChatAvatar Player;
        public readonly uint Error;
        public readonly uint RoomId;
        public readonly uint RequestId;
        ChatOnEnteredRoomPacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 5))
                throw new InvalidDataException("Unexpected ChatOnEnteredRoomPacket header");
            Player = ChatAvatar.ReadFrom(reader);
            Error = reader.U32();
            RoomId = reader.U32();
            RequestId = reader.U32();
            PacketFieldCodec.End(reader);
        }

        public static ChatOnEnteredRoomPacket ReadFrom(ByteReader reader) => new ChatOnEnteredRoomPacket(reader);
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
