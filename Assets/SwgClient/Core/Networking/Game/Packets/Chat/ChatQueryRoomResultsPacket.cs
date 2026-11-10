using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Chat
{
    // Core3/MMOCoreORB/src/server/zone/packets/chat/ChatQueryRoomResults.h:14 (ChatQueryRoomResults).
    public readonly struct ChatQueryRoomResultsPacket : IPacket
    {
        public const uint Opcode = 0xc4de864e;
        public readonly ushort OperandCount;
        public readonly ChatAvatar[] Players;
        public readonly ChatAvatar[] Invited;
        public readonly ChatAvatar[] Moderators;
        public readonly ChatAvatar[] Banned;
        public readonly uint RequestId;
        public readonly ChatRoomData Room;
        ChatQueryRoomResultsPacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 7))
                throw new InvalidDataException("Unexpected ChatQueryRoomResultsPacket header");
            Players = new ChatAvatar[PacketFieldCodec.Count(reader, reader.U32(), 6)];
            for (int i = 0; i < Players.Length; i++)
                Players[i] = ChatAvatar.ReadFrom(reader);
            Invited = new ChatAvatar[PacketFieldCodec.Count(reader, reader.U32(), 6)];
            for (int i = 0; i < Invited.Length; i++)
                Invited[i] = ChatAvatar.ReadFrom(reader);
            Moderators = new ChatAvatar[PacketFieldCodec.Count(reader, reader.U32(), 6)];
            for (int i = 0; i < Moderators.Length; i++)
                Moderators[i] = ChatAvatar.ReadFrom(reader);
            Banned = new ChatAvatar[PacketFieldCodec.Count(reader, reader.U32(), 6)];
            for (int i = 0; i < Banned.Length; i++)
                Banned[i] = ChatAvatar.ReadFrom(reader);
            RequestId = reader.U32();
            Room = ChatRoomData.ReadFrom(reader);
            PacketFieldCodec.End(reader);
        }

        public static ChatQueryRoomResultsPacket ReadFrom(ByteReader reader) => new ChatQueryRoomResultsPacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write(checked((uint)Players.Length));
            foreach (var item in Players)
                item.WriteTo(writer);
            writer.Write(checked((uint)Invited.Length));
            foreach (var item in Invited)
                item.WriteTo(writer);
            writer.Write(checked((uint)Moderators.Length));
            foreach (var item in Moderators)
                item.WriteTo(writer);
            writer.Write(checked((uint)Banned.Length));
            foreach (var item in Banned)
                item.WriteTo(writer);
            writer.Write(RequestId);
            Room.WriteTo(writer);
        }
    }
}
