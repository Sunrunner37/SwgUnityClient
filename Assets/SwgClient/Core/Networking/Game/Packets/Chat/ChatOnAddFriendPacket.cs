using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Chat
{
    // Core3/MMOCoreORB/src/server/zone/packets/chat/ChatOnAddFriend.h:10 (ChatOnAddFriend).
    public readonly struct ChatOnAddFriendPacket : IPacket
    {
        public const uint Opcode = 0x2b2a0d94;
        public readonly ushort OperandCount;
        public readonly ulong Reserved;
        ChatOnAddFriendPacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 3))
                throw new InvalidDataException("Unexpected ChatOnAddFriendPacket header");
            Reserved = reader.U64();
            PacketFieldCodec.End(reader);
        }

        public static ChatOnAddFriendPacket ReadFrom(ByteReader reader) => new ChatOnAddFriendPacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write(Reserved);
        }
    }
}
