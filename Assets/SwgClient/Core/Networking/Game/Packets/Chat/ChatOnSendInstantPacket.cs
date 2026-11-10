using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Chat
{
    // Core3/MMOCoreORB/src/server/zone/packets/chat/ChatOnSendInstantMessage.h:10 (ChatOnSendInstantMessage).
    public readonly struct ChatOnSendInstantPacket : IPacket
    {
        public const uint Opcode = 0x88dbb381;
        public readonly ushort OperandCount;
        public readonly uint Result;
        public readonly uint Sequence;
        ChatOnSendInstantPacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 3))
                throw new InvalidDataException("Unexpected ChatOnSendInstantPacket header");
            Result = reader.U32();
            Sequence = reader.U32();
            PacketFieldCodec.End(reader);
        }

        public static ChatOnSendInstantPacket ReadFrom(ByteReader reader) => new ChatOnSendInstantPacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write(Result);
            writer.Write(Sequence);
        }
    }
}
