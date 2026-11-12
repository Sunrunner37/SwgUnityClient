using System.IO;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Chat
{
    public readonly struct ChatEnterRoomByIdPacket : IPacket
    {
        public const uint Opcode = 0xbc6bddf2;
        public readonly uint RequestId;
        public readonly uint RoomId;

        public ChatEnterRoomByIdPacket(uint requestId, uint roomId)
        {
            RequestId = requestId;
            RoomId = roomId;
        }

        public void WriteTo(BinaryWriter writer)
        {
            writer.Write((ushort)2);
            writer.Write(Opcode);
            writer.Write(RequestId);
            writer.Write(RoomId);
        }
    }
}
