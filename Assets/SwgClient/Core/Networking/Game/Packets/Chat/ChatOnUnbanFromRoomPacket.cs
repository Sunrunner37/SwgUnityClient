using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Chat
{
    // Core3/MMOCoreORB/src/server/zone/packets/chat/ChatOnUnbanFromRoom.h:13 (ChatOnUnbanFromRoom).
    public readonly struct ChatOnUnbanFromRoomPacket : IPacket
    {
        public const uint Opcode = 0xbaf9b815;
        public readonly ushort OperandCount;
        public readonly string RoomPath;
        public readonly ChatAvatar Actor;
        public readonly ChatAvatar Target;
        public readonly uint Error;
        public readonly uint RequestId;
        ChatOnUnbanFromRoomPacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 6))
                throw new InvalidDataException("Unexpected ChatOnUnbanFromRoomPacket header");
            RoomPath = reader.Ascii();
            Actor = ChatAvatar.ReadFrom(reader);
            Target = ChatAvatar.ReadFrom(reader);
            Error = reader.U32();
            RequestId = reader.U32();
            PacketFieldCodec.End(reader);
        }

        public static ChatOnUnbanFromRoomPacket ReadFrom(ByteReader reader) => new ChatOnUnbanFromRoomPacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            PacketFieldCodec.Ascii(writer, RoomPath);
            Actor.WriteTo(writer);
            Target.WriteTo(writer);
            writer.Write(Error);
            writer.Write(RequestId);
        }
    }
}
