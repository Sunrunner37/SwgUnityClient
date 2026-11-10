using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Chat
{
    // Core3/MMOCoreORB/src/server/zone/packets/chat/ChatSystemMessage.h:11 (ChatSystemMessage).
    public readonly struct ChatSystemPacket : IPacket
    {
        public const uint Opcode = 0x6d2a6413;
        public readonly ushort OperandCount;
        public readonly byte DisplayType;
        public readonly string Message;
        public readonly ChatParameterBlock Parameters;
        ChatSystemPacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 4))
                throw new InvalidDataException("Unexpected ChatSystemPacket header");
            DisplayType = reader.Byte();
            Message = reader.Unicode();
            Parameters = ChatParameterBlock.ReadFrom(reader);
            PacketFieldCodec.End(reader);
        }

        public static ChatSystemPacket ReadFrom(ByteReader reader) => new ChatSystemPacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write(DisplayType);
            PacketFieldCodec.Unicode(writer, Message);
            Parameters.WriteTo(writer);
        }
    }
}
