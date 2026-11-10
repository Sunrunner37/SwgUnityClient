using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Chat
{
    // Core3/MMOCoreORB/src/server/zone/packets/chat/ChatServerStatus.h:13 (ChatServerStatus).
    // Core3/MMOCoreORB/src/server/zone/packets/zone/unkByteFlag.h:10 (unkByteFlag).
    public readonly struct ChatServerStatusPacket : IPacket
    {
        public const uint Opcode = 0x7102b15f;
        public readonly ushort OperandCount;
        public readonly byte Flag;
        ChatServerStatusPacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 2))
                throw new InvalidDataException("Unexpected ChatServerStatusPacket header");
            Flag = reader.Byte();
            PacketFieldCodec.End(reader);
        }

        public static ChatServerStatusPacket ReadFrom(ByteReader reader) => new ChatServerStatusPacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write(Flag);
        }
    }
}
