using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Session
{
    // Core3/MMOCoreORB/src/server/zone/packets/zone/GalaxyLoopTimes.h:14 (GalaxyLoopTimesResponse).
    public readonly struct GalaxyLoopTimesResponsePacket : IPacket
    {
        public const uint Opcode = 0x4e428088;
        public readonly ushort OperandCount;
        public readonly uint GameLoopTime;
        public readonly uint ObjectLoopTime;
        public readonly uint Reserved;
        GalaxyLoopTimesResponsePacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 3))
                throw new InvalidDataException("Unexpected GalaxyLoopTimesResponsePacket header");
            GameLoopTime = reader.U32();
            ObjectLoopTime = reader.U32();
            Reserved = reader.U32();
            PacketFieldCodec.End(reader);
        }

        public static GalaxyLoopTimesResponsePacket ReadFrom(ByteReader reader) => new GalaxyLoopTimesResponsePacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write(GameLoopTime);
            writer.Write(ObjectLoopTime);
            writer.Write(Reserved);
        }
    }
}
