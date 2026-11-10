using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Chat
{
    // Core3/MMOCoreORB/src/server/zone/packets/chat/ChatInstantMessageToClient.h:10 (ChatInstantMessageToClient).
    public readonly struct ChatInstantMessageToClientPacket : IPacket
    {
        public const uint Opcode = 0x3c565ced;
        public readonly ushort OperandCount;
        public readonly ChatAvatar Sender;
        public readonly string Message;
        public readonly uint Reserved;
        ChatInstantMessageToClientPacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 5))
                throw new InvalidDataException("Unexpected ChatInstantMessageToClientPacket header");
            Sender = ChatAvatar.ReadFrom(reader);
            Message = reader.Unicode();
            Reserved = reader.U32();
            PacketFieldCodec.End(reader);
        }

        public static ChatInstantMessageToClientPacket ReadFrom(ByteReader reader) => new ChatInstantMessageToClientPacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            Sender.WriteTo(writer);
            PacketFieldCodec.Unicode(writer, Message);
            writer.Write(Reserved);
        }
    }
}
