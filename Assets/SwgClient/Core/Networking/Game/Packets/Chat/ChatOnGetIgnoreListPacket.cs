using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Chat
{
    // Core3/MMOCoreORB/src/server/zone/packets/chat/ChatOnGetIgnoreList.h:11 (ChatOnGetIgnoreList).
    public readonly struct ChatOnGetIgnoreListPacket : IPacket
    {
        public const uint Opcode = 0xf8c275b0;
        public readonly ushort OperandCount;
        public readonly ulong PlayerSwgId;
        public readonly ChatAvatar[] Players;
        ChatOnGetIgnoreListPacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 3))
                throw new InvalidDataException("Unexpected ChatOnGetIgnoreListPacket header");
            PlayerSwgId = reader.U64();
            Players = new ChatAvatar[PacketFieldCodec.Count(reader, reader.U32(), 6)];
            for (int i = 0; i < Players.Length; i++)
                Players[i] = ChatAvatar.ReadFrom(reader);
            PacketFieldCodec.End(reader);
        }

        public static ChatOnGetIgnoreListPacket ReadFrom(ByteReader reader) => new ChatOnGetIgnoreListPacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write(PlayerSwgId);
            writer.Write(checked((uint)Players.Length));
            foreach (var item in Players)
                item.WriteTo(writer);
        }
    }
}
