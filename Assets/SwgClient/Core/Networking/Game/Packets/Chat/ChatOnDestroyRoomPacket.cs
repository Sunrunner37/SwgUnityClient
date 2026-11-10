using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Chat
{
    // Core3/MMOCoreORB/src/server/zone/packets/chat/ChatOnDestroyRoom.h:8 (ChatOnDestroyRoom).
    public readonly struct ChatOnDestroyRoomPacket : IPacket
    {
        public const uint Opcode = 0xe8ec5877;
        public readonly ushort OperandCount;
        public readonly ChatAvatar Player;
        public readonly uint Error;
        public readonly uint RoomId;
        public readonly uint RequestId;
        ChatOnDestroyRoomPacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 5))
                throw new InvalidDataException("Unexpected ChatOnDestroyRoomPacket header");
            Player = ChatAvatar.ReadFrom(reader);
            Error = reader.U32();
            RoomId = reader.U32();
            RequestId = reader.U32();
            PacketFieldCodec.End(reader);
        }

        public static ChatOnDestroyRoomPacket ReadFrom(ByteReader reader) => new ChatOnDestroyRoomPacket(reader);
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
