using System.IO;
using UnityEngine;
using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Networking.Game.Packets.Data
{
    public readonly struct ChatWaypoint
    {
        public readonly uint Reserved;
        public readonly Vector3 Position;
        public readonly ulong PointerId;
        public readonly uint PlanetCrc;
        public readonly string Name;
        public readonly ulong CellId;
        public readonly byte Color;
        public readonly byte Active;
        ChatWaypoint(ByteReader reader)
        {
            this = default;
            Reserved = reader.U32();
            Position = PacketFieldCodec.Position(reader);
            PointerId = reader.U64();
            PlanetCrc = reader.U32();
            Name = reader.Unicode();
            CellId = reader.U64();
            Color = reader.Byte();
            Active = reader.Byte();
        }

        public static ChatWaypoint ReadFrom(ByteReader reader) => new ChatWaypoint(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(Reserved);
            PacketFieldCodec.Position(writer, Position);
            writer.Write(PointerId);
            writer.Write(PlanetCrc);
            PacketFieldCodec.Unicode(writer, Name);
            writer.Write(CellId);
            writer.Write(Color);
            writer.Write(Active);
        }
    }
}
