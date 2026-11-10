using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Session
{
    // Core3/MMOCoreORB/src/server/zone/packets/zone/ConnectionServerTestMessage.h:13 (ConnectionServerTestMessage).
    public readonly struct ConnectionServerTestPacket : IPacket
    {
        public const uint Opcode = 0xe20d7e9;
        public readonly ushort OperandCount;
        public readonly string ServerName;
        ConnectionServerTestPacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 2))
                throw new InvalidDataException("Unexpected ConnectionServerTestPacket header");
            ServerName = reader.Ascii();
            PacketFieldCodec.End(reader);
        }

        public static ConnectionServerTestPacket ReadFrom(ByteReader reader) => new ConnectionServerTestPacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            PacketFieldCodec.Ascii(writer, ServerName);
        }
    }
}
