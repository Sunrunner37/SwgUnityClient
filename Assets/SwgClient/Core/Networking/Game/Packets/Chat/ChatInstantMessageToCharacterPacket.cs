using System.IO;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Chat
{
    public readonly struct ChatInstantMessageToCharacterPacket : IPacket
    {
        public const uint Opcode = 0x84bb21f7;
        public readonly string Galaxy;
        public readonly string Recipient;
        public readonly string Message;
        public readonly uint Sequence;

        public ChatInstantMessageToCharacterPacket(string galaxy, string recipient, string message, uint sequence)
        {
            Galaxy = galaxy;
            Recipient = recipient;
            Message = message;
            Sequence = sequence;
        }

        public void WriteTo(BinaryWriter writer)
        {
            writer.Write((ushort)6);
            writer.Write(Opcode);
            PacketFieldCodec.Ascii(writer, "SWG");
            PacketFieldCodec.Ascii(writer, Galaxy);
            PacketFieldCodec.Ascii(writer, Recipient);
            PacketFieldCodec.Unicode(writer, Message);
            writer.Write(0u);
            writer.Write(Sequence);
        }
    }
}
