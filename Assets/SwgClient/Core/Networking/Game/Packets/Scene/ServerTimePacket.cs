using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Scene
{
    // Core3/MMOCoreORB/src/server/zone/packets/scene/ServerTimeMessage.h:12 (ServerTimeMessage).
    public readonly struct ServerTimePacket : IPacket
    {
        public const uint Opcode = 0x2ebc3bd9;
        public readonly ushort OperandCount;
        public readonly ulong Time;
        ServerTimePacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 2))
                throw new InvalidDataException("Unexpected ServerTimePacket header");
            Time = reader.U64();
            PacketFieldCodec.End(reader);
        }

        public static ServerTimePacket ReadFrom(ByteReader reader) => new ServerTimePacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write(Time);
        }
    }
}
