using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Chat
{
    // Core3/MMOCoreORB/src/server/zone/packets/chat/ChatOnAddModeratorToRoom.h:13 (ChatOnAddModeratorToRoom).
    public readonly struct ChatOnAddModeratorToRoomPacket : IPacket
    {
        public const uint Opcode = 0x36a03858;
        public readonly ushort OperandCount;
        public readonly ChatAvatar Target;
        public readonly ChatAvatar Actor;
        public readonly uint Error;
        public readonly string RoomPath;
        public readonly uint RequestId;
        ChatOnAddModeratorToRoomPacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 6))
                throw new InvalidDataException("Unexpected ChatOnAddModeratorToRoomPacket header");
            Target = ChatAvatar.ReadFrom(reader);
            Actor = ChatAvatar.ReadFrom(reader);
            Error = reader.U32();
            RoomPath = reader.Ascii();
            RequestId = reader.U32();
            PacketFieldCodec.End(reader);
        }

        public static ChatOnAddModeratorToRoomPacket ReadFrom(ByteReader reader) => new ChatOnAddModeratorToRoomPacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            Target.WriteTo(writer);
            Actor.WriteTo(writer);
            writer.Write(Error);
            PacketFieldCodec.Ascii(writer, RoomPath);
            writer.Write(RequestId);
        }
    }
}
