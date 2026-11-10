using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Player
{
    // Core3/MMOCoreORB/src/server/zone/packets/player/GetMapLocationsResponseMessage.h:12 (GetMapLocationsResponseMessage).
    public readonly struct GetMapLocationsResponsePacket : IPacket
    {
        public const uint Opcode = 0x9f80464c;
        public readonly ushort OperandCount;
        public readonly string Planet;
        public readonly MapLocation[] Locations;
        public readonly uint[] Reserved;
        GetMapLocationsResponsePacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 5))
                throw new InvalidDataException("Unexpected GetMapLocationsResponsePacket header");
            Planet = reader.Ascii();
            Locations = new MapLocation[PacketFieldCodec.Count(reader, reader.U32(), 23)];
            for (int i = 0; i < Locations.Length; i++)
                Locations[i] = MapLocation.ReadFrom(reader);
            Reserved = new uint[5];
            for (int i = 0; i < Reserved.Length; i++)
                Reserved[i] = reader.U32();
            PacketFieldCodec.End(reader);
        }

        public static GetMapLocationsResponsePacket ReadFrom(ByteReader reader) => new GetMapLocationsResponsePacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            PacketFieldCodec.Ascii(writer, Planet);
            writer.Write(checked((uint)Locations.Length));
            foreach (var item in Locations)
                item.WriteTo(writer);
            foreach (var item in Reserved)
                writer.Write(item);
        }
    }
}
