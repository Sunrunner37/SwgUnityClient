using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Chat
{
    // Core3/MMOCoreORB/src/server/zone/packets/chat/ChatOnChangeIgnoreStatus.h:10 (ChatOnChangeIgnoreStatus).
    public readonly struct ChatOnChangeIgnoreStatusPacket : IPacket
    {
        public const uint Opcode = 0x70e9da0f;
        public readonly ushort OperandCount;
        public readonly ulong ObjectId;
        public readonly ChatAvatar Ignored;
        public readonly uint Reserved1;
        public readonly byte Added;
        public readonly uint Reserved2;
        ChatOnChangeIgnoreStatusPacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 6))
                throw new InvalidDataException("Unexpected ChatOnChangeIgnoreStatusPacket header");
            ObjectId = reader.U64();
            Ignored = ChatAvatar.ReadFrom(reader);
            Reserved1 = reader.U32();
            Added = reader.Byte();
            Reserved2 = reader.U32();
            PacketFieldCodec.End(reader);
        }

        public static ChatOnChangeIgnoreStatusPacket ReadFrom(ByteReader reader) => new ChatOnChangeIgnoreStatusPacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write(ObjectId);
            Ignored.WriteTo(writer);
            writer.Write(Reserved1);
            writer.Write(Added);
            writer.Write(Reserved2);
        }
    }
}
