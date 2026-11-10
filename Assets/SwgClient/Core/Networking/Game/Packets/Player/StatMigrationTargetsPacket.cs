using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Player
{
    // Core3/MMOCoreORB/src/server/zone/packets/player/StatMigrationTargetsMessage.h:10 (StatMigrationTargetsMessage).
    public readonly struct StatMigrationTargetsPacket : IPacket
    {
        public const uint Opcode = 0xefac38c4;
        public readonly ushort OperandCount;
        public readonly int[] Attributes;
        public readonly int RemainingPoints;
        StatMigrationTargetsPacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 9))
                throw new InvalidDataException("Unexpected StatMigrationTargetsPacket header");
            Attributes = new int[9];
            for (int i = 0; i < Attributes.Length; i++)
                Attributes[i] = unchecked((int)reader.U32());
            RemainingPoints = unchecked((int)reader.U32());
            PacketFieldCodec.End(reader);
        }

        public static StatMigrationTargetsPacket ReadFrom(ByteReader reader) => new StatMigrationTargetsPacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            foreach (var item in Attributes)
                writer.Write(item);
            writer.Write(RemainingPoints);
        }
    }
}
