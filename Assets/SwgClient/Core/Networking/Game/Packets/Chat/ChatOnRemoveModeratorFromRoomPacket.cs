using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Chat
{
    // Core3/MMOCoreORB/src/server/zone/packets/chat/ChatOnRemoveModeratorFromRoom.h:13 (ChatOnRemoveModeratorFromRoom).
    public readonly struct ChatOnRemoveModeratorFromRoomPacket : IPacket
    {
        public const uint Opcode = 0x1342fc47;
        public readonly ushort OperandCount;
        public readonly ChatAvatar Target;
        public readonly ChatAvatar Actor;
        public readonly uint Error;
        public readonly string RoomPath;
        public readonly uint RequestId;
        ChatOnRemoveModeratorFromRoomPacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 6))
                throw new InvalidDataException("Unexpected ChatOnRemoveModeratorFromRoomPacket header");
            Target = ChatAvatar.ReadFrom(reader);
            Actor = ChatAvatar.ReadFrom(reader);
            Error = reader.U32();
            RoomPath = reader.Ascii();
            RequestId = reader.U32();
            PacketFieldCodec.End(reader);
        }

        public static ChatOnRemoveModeratorFromRoomPacket ReadFrom(ByteReader reader) => new ChatOnRemoveModeratorFromRoomPacket(reader);
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
