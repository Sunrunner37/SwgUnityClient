using System.IO;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Chat
{
    public readonly struct ChatRequestRoomListPacket : IPacket
    {
        public const uint Opcode = 0x4c3d2cfa;


        public void WriteTo(BinaryWriter writer)
        {
            writer.Write((ushort)1);
            writer.Write(Opcode);

        }
    }
}
