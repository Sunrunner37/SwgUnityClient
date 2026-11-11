using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Chat
{
    // Core3/MMOCoreORB/src/server/zone/packets/chat/ChatOnChangeFriendStatus.h:10 (ChatOnChangeFriendStatus).
    public readonly struct ChatOnChangeFriendStatusPacket : IPacket
    {
        public const uint Opcode = 0x54336726;
        public readonly ushort OperandCount;
        public readonly ulong SwgId;
        public readonly ChatAvatar Friend;
        public readonly uint Reserved1;
        public readonly uint Added;
        public readonly byte Reserved2;
        ChatOnChangeFriendStatusPacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 6))
                throw new InvalidDataException("Unexpected ChatOnChangeFriendStatusPacket header");
            SwgId = reader.U64();
            Friend = ChatAvatar.ReadFrom(reader);
            Reserved1 = reader.U32();
            Added = reader.U32();
            Reserved2 = reader.Byte();
            PacketFieldCodec.End(reader);
        }

        public static ChatOnChangeFriendStatusPacket ReadFrom(ByteReader reader) => new ChatOnChangeFriendStatusPacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write(SwgId);
            Friend.WriteTo(writer);
            writer.Write(Reserved1);
            writer.Write(Added);
            writer.Write(Reserved2);
        }
    }
}
