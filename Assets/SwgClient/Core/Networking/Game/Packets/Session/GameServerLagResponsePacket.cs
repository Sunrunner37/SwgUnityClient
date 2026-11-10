using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Session
{
    // Core3/MMOCoreORB/src/server/zone/packets/zone/LagReport.h:14 (GameServerLagResponse).
    public readonly struct GameServerLagResponsePacket : IPacket
    {
        public const uint Opcode = 0x789a4e0a;
        public readonly ushort OperandCount;
        GameServerLagResponsePacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 1))
                throw new InvalidDataException("Unexpected GameServerLagResponsePacket header");
            PacketFieldCodec.End(reader);
        }

        public static GameServerLagResponsePacket ReadFrom(ByteReader reader) => new GameServerLagResponsePacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
        }
    }
}
