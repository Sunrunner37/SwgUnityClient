using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Session
{
    // Core3/MMOCoreORB/src/server/zone/packets/HeartBeat.h:12 (HeartBeat).
    // Core3/MMOCoreORB/src/server/zone/packets/LinkDeadMessage.h:10 (LinkDeadMessage).
    public readonly struct HeartBeatPacket : IPacket
    {
        public const uint Opcode = 0xa16cf9af;
        public readonly ushort OperandCount;
        HeartBeatPacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 1 && OperandCount != 5))
                throw new InvalidDataException("Unexpected HeartBeatPacket header");
            PacketFieldCodec.End(reader);
        }

        public static HeartBeatPacket ReadFrom(ByteReader reader) => new HeartBeatPacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
        }
    }
}
