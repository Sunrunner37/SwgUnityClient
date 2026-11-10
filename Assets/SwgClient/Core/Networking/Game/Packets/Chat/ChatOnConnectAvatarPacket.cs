using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Chat
{
    // Core3/MMOCoreORB/src/server/zone/packets/chat/ChatOnConnectAvatar.h:13 (ChatOnConnectAvatar).
    public readonly struct ChatOnConnectAvatarPacket : IPacket
    {
        public const uint Opcode = 0xd72fe9be;
        public readonly ushort OperandCount;
        ChatOnConnectAvatarPacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 1))
                throw new InvalidDataException("Unexpected ChatOnConnectAvatarPacket header");
            PacketFieldCodec.End(reader);
        }

        public static ChatOnConnectAvatarPacket ReadFrom(ByteReader reader) => new ChatOnConnectAvatarPacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
        }
    }
}
