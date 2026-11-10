using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Session
{
    // Core3/MMOCoreORB/src/server/zone/packets/zone/LagReport.h:23 (ConnectionServerLagResponse).
    public readonly struct ConnectionServerLagResponsePacket : IPacket
    {
        public const uint Opcode = 0x1590f63c;
        public readonly ushort OperandCount;
        ConnectionServerLagResponsePacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 1))
                throw new InvalidDataException("Unexpected ConnectionServerLagResponsePacket header");
            PacketFieldCodec.End(reader);
        }

        public static ConnectionServerLagResponsePacket ReadFrom(ByteReader reader) => new ConnectionServerLagResponsePacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
        }
    }
}
