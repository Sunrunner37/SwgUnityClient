using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Chat
{
    // Core3/MMOCoreORB/src/server/zone/packets/chat/ChatOnGetFriendsList.h:11 (ChatOnGetFriendsList).
    public readonly struct ChatOnGetFriendsListPacket : IPacket
    {
        public const uint Opcode = 0xe97ab594;
        public readonly ushort OperandCount;
        public readonly ulong PlayerId;
        public readonly ChatAvatar[] Players;
        ChatOnGetFriendsListPacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 3))
                throw new InvalidDataException("Unexpected ChatOnGetFriendsListPacket header");
            PlayerId = reader.U64();
            Players = new ChatAvatar[PacketFieldCodec.Count(reader, reader.U32(), 6)];
            for (int i = 0; i < Players.Length; i++)
                Players[i] = ChatAvatar.ReadFrom(reader);
            PacketFieldCodec.End(reader);
        }

        public static ChatOnGetFriendsListPacket ReadFrom(ByteReader reader) => new ChatOnGetFriendsListPacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write(PlayerId);
            writer.Write(checked((uint)Players.Length));
            foreach (var item in Players)
                item.WriteTo(writer);
        }
    }
}
