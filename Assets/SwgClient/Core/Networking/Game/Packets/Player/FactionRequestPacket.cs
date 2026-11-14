using System.IO;
namespace Swg.Client.Core.Networking.Game.Packets.Player
{
    // Core3 FactionRequestMessageCallback has no payload.
    public readonly struct FactionRequestPacket : IPacket
    {
        public const uint Opcode = 0xc1b03b81;
        public void WriteTo(BinaryWriter writer) { writer.Write((ushort)1); writer.Write(Opcode); }
    }
}
