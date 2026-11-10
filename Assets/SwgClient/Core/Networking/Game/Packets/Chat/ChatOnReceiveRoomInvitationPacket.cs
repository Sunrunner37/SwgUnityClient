using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Chat
{
    // Core3/MMOCoreORB/src/server/zone/packets/chat/ChatOnReceiveRoomInvitation.h:10 (ChatOnReceiveRoomInvitation).
    public readonly struct ChatOnReceiveRoomInvitationPacket : IPacket
    {
        public const uint Opcode = 0xc17eb06d;
        public readonly ushort OperandCount;
        public readonly ChatAvatar Moderator;
        public readonly string RoomPath;
        ChatOnReceiveRoomInvitationPacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 3))
                throw new InvalidDataException("Unexpected ChatOnReceiveRoomInvitationPacket header");
            Moderator = ChatAvatar.ReadFrom(reader);
            RoomPath = reader.Ascii();
            PacketFieldCodec.End(reader);
        }

        public static ChatOnReceiveRoomInvitationPacket ReadFrom(ByteReader reader) => new ChatOnReceiveRoomInvitationPacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            Moderator.WriteTo(writer);
            PacketFieldCodec.Ascii(writer, RoomPath);
        }
    }
}
