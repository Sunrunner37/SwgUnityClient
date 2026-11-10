using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Chat
{
    // Core3/MMOCoreORB/src/server/zone/packets/chat/ChatOnInviteToRoom.h:13 (ChatOnInviteToRoom).
    public readonly struct ChatOnInviteToRoomPacket : IPacket
    {
        public const uint Opcode = 0x493fe74a;
        public readonly ushort OperandCount;
        public readonly ChatAvatar Inviter;
        public readonly ChatAvatar Invitee;
        public readonly string RoomPath;
        public readonly uint Error;
        public readonly uint RequestId;
        ChatOnInviteToRoomPacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 5))
                throw new InvalidDataException("Unexpected ChatOnInviteToRoomPacket header");
            Inviter = ChatAvatar.ReadFrom(reader);
            Invitee = ChatAvatar.ReadFrom(reader);
            RoomPath = reader.Ascii();
            Error = reader.U32();
            RequestId = reader.U32();
            PacketFieldCodec.End(reader);
        }

        public static ChatOnInviteToRoomPacket ReadFrom(ByteReader reader) => new ChatOnInviteToRoomPacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            Inviter.WriteTo(writer);
            Invitee.WriteTo(writer);
            PacketFieldCodec.Ascii(writer, RoomPath);
            writer.Write(Error);
            writer.Write(RequestId);
        }
    }
}
