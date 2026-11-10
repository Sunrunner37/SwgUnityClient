using System.IO;
using UnityEngine;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Effects
{
    // Core3/MMOCoreORB/src/server/zone/packets/scene/PlayClientEffectLocMessage.h:10 (PlayClientEffectLoc).
    public readonly struct PlayClientEffectLocPacket : IPacket
    {
        public const uint Opcode = 0x2949e74;
        public readonly ushort OperandCount;
        public readonly string EffectFile;
        public readonly string Planet;
        public readonly Vector3 Position;
        public readonly ulong CellId;
        public readonly int TerrainDelta;
        public readonly string Label;
        PlayClientEffectLocPacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 9))
                throw new InvalidDataException("Unexpected PlayClientEffectLocPacket header");
            EffectFile = reader.Ascii();
            Planet = reader.Ascii();
            Position = PacketFieldCodec.Position(reader);
            CellId = reader.U64();
            TerrainDelta = unchecked((int)reader.U32());
            Label = reader.Ascii();
            PacketFieldCodec.End(reader);
        }

        public static PlayClientEffectLocPacket ReadFrom(ByteReader reader) => new PlayClientEffectLocPacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            PacketFieldCodec.Ascii(writer, EffectFile);
            PacketFieldCodec.Ascii(writer, Planet);
            PacketFieldCodec.Position(writer, Position);
            writer.Write(CellId);
            writer.Write(TerrainDelta);
            PacketFieldCodec.Ascii(writer, Label);
        }
    }
}
