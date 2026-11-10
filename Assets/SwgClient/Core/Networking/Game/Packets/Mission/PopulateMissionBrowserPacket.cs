using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Mission
{
    // Core3/MMOCoreORB/src/server/zone/packets/mission/PopulateMissionBrowserMessage.h:10 (PopulateMissionBrowserMessage).
    public readonly struct PopulateMissionBrowserPacket : IPacket
    {
        public const uint Opcode = 0x88d9885c;
        public readonly ushort OperandCount;
        public readonly uint Reserved;
        PopulateMissionBrowserPacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 3))
                throw new InvalidDataException("Unexpected PopulateMissionBrowserPacket header");
            Reserved = reader.U32();
            PacketFieldCodec.End(reader);
        }

        public static PopulateMissionBrowserPacket ReadFrom(ByteReader reader) => new PopulateMissionBrowserPacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write(Reserved);
        }
    }
}
