using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Player
{
    // Core3/MMOCoreORB/src/server/zone/packets/player/FactionResponseMessage.h:10 (FactionResponseMessage).
    public readonly struct FactionResponsePacket : IPacket
    {
        public const uint Opcode = 0x5dd53957;
        public readonly ushort OperandCount;
        public readonly string Rank;
        public readonly int RebelPoints;
        public readonly int ImperialPoints;
        public readonly int HuttPoints;
        public readonly string[] FactionNames;
        public readonly float[] Standings;
        FactionResponsePacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 7))
                throw new InvalidDataException("Unexpected FactionResponsePacket header");
            Rank = reader.Ascii();
            RebelPoints = unchecked((int)reader.U32());
            ImperialPoints = unchecked((int)reader.U32());
            HuttPoints = unchecked((int)reader.U32());
            FactionNames = new string[PacketFieldCodec.Count(reader, reader.U32(), 2)];
            for (int i = 0; i < FactionNames.Length; i++)
                FactionNames[i] = reader.Ascii();
            Standings = new float[PacketFieldCodec.Count(reader, reader.U32(), 4)];
            for (int i = 0; i < Standings.Length; i++)
                Standings[i] = reader.Float();
            PacketFieldCodec.End(reader);
        }

        public static FactionResponsePacket ReadFrom(ByteReader reader) => new FactionResponsePacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            PacketFieldCodec.Ascii(writer, Rank);
            writer.Write(RebelPoints);
            writer.Write(ImperialPoints);
            writer.Write(HuttPoints);
            writer.Write(checked((uint)FactionNames.Length));
            foreach (var item in FactionNames)
                PacketFieldCodec.Ascii(writer, item);
            writer.Write(checked((uint)Standings.Length));
            foreach (var item in Standings)
                writer.Write(item);
        }
    }
}
