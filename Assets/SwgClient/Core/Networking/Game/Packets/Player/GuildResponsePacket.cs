using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Player
{
    // Core3/MMOCoreORB/src/server/zone/packets/player/GuildResponseMessage.h:12 (GuildResponseMessage).
    public readonly struct GuildResponsePacket : IPacket
    {
        public const uint Opcode = 0x32263f20;
        public readonly ushort OperandCount;
        public readonly ulong SwgId;
        public readonly string GuildName;
        public readonly ushort Reserved;
        GuildResponsePacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 4))
                throw new InvalidDataException("Unexpected GuildResponsePacket header");
            SwgId = reader.U64();
            GuildName = reader.Ascii();
            Reserved = reader.U16();
            PacketFieldCodec.End(reader);
        }

        public static GuildResponsePacket ReadFrom(ByteReader reader) => new GuildResponsePacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write(SwgId);
            PacketFieldCodec.Ascii(writer, GuildName);
            writer.Write(Reserved);
        }
    }
}
