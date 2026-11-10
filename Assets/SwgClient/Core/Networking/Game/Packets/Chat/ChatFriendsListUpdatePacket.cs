using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Chat
{
    // Core3/MMOCoreORB/src/server/zone/packets/chat/ChatFriendsListUpdate.h:10 (ChatFriendsListUpdate).
    public readonly struct ChatFriendsListUpdatePacket : IPacket
    {
        public const uint Opcode = 0x6cd2fcd8;
        public readonly ushort OperandCount;
        public readonly ChatAvatar Friend;
        public readonly byte Online;
        ChatFriendsListUpdatePacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 3))
                throw new InvalidDataException("Unexpected ChatFriendsListUpdatePacket header");
            Friend = ChatAvatar.ReadFrom(reader);
            Online = reader.Byte();
            PacketFieldCodec.End(reader);
        }

        public static ChatFriendsListUpdatePacket ReadFrom(ByteReader reader) => new ChatFriendsListUpdatePacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            Friend.WriteTo(writer);
            writer.Write(Online);
        }
    }
}
