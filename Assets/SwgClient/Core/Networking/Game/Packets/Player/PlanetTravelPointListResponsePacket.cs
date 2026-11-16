using System.IO;
using UnityEngine;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Player
{
    // Core3/MMOCoreORB/src/server/zone/packets/player/PlanetTravelPointListResponse.h:35 (PlanetTravelPointListResponse).
    public readonly struct PlanetTravelPointListResponsePacket : IPacket
    {
        public const uint Opcode = 0x4d32541f;
        public readonly ushort OperandCount;
        public readonly string Planet;
        public readonly string[] Names;
        public readonly Vector3[] Positions;
        public readonly uint[] Taxes;
        public readonly byte[] Starports;
        PlanetTravelPointListResponsePacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 6))
                throw new InvalidDataException("Unexpected PlanetTravelPointListResponsePacket header");
            Planet = reader.Ascii();
            Names = new string[PacketFieldCodec.Count(reader, reader.U32(), 2)];
            for (int i = 0; i < Names.Length; i++)
                Names[i] = reader.Ascii();
            Positions = new Vector3[PacketFieldCodec.Count(reader, reader.U32(), 12)];
            for (int i = 0; i < Positions.Length; i++)
                Positions[i] = PacketFieldCodec.Position(reader);
            Taxes = new uint[PacketFieldCodec.Count(reader, reader.U32(), 4)];
            for (int i = 0; i < Taxes.Length; i++)
                Taxes[i] = reader.U32();
            Starports = new byte[PacketFieldCodec.Count(reader, reader.U32(), 1)];
            for (int i = 0; i < Starports.Length; i++)
                Starports[i] = reader.Byte();
            if (Names.Length != Positions.Length || Names.Length != Taxes.Length || Names.Length != Starports.Length)
                throw new InvalidDataException("Travel destination columns have different lengths");
            PacketFieldCodec.End(reader);
        }

        public static PlanetTravelPointListResponsePacket ReadFrom(ByteReader reader) => new PlanetTravelPointListResponsePacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            PacketFieldCodec.Ascii(writer, Planet);
            writer.Write(checked((uint)Names.Length));
            foreach (var item in Names)
                PacketFieldCodec.Ascii(writer, item);
            writer.Write(checked((uint)Positions.Length));
            foreach (var item in Positions)
                PacketFieldCodec.Position(writer, item);
            writer.Write(checked((uint)Taxes.Length));
            foreach (var item in Taxes)
                writer.Write(item);
            writer.Write(checked((uint)Starports.Length));
            foreach (var item in Starports)
                writer.Write(item);
        }
    }
}
