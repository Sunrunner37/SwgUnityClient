using System.IO;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Chat
{
    public readonly struct ChatSendToRoomPacket : IPacket
    {
        public const uint Opcode = 0x20e4dbe3;
        public readonly string Message;
        public readonly uint RoomId;
        public readonly uint Sequence;

        public ChatSendToRoomPacket(string message, uint roomId, uint sequence)
        {
            Message = message;
            RoomId = roomId;
            Sequence = sequence;
        }

        public void WriteTo(BinaryWriter writer)
        {
            writer.Write((ushort)4);
            writer.Write(Opcode);
            PacketFieldCodec.Unicode(writer, Message);
            writer.Write(0u);
            writer.Write(RoomId);
            writer.Write(Sequence);
        }
    }
}
