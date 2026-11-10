using System.IO;
using UnityEngine;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Ui
{
    // Core3/MMOCoreORB/src/server/zone/packets/ui/CreateClientPathMessage.h:13 (CreateClientPathMessage).
    public readonly struct CreateClientPathPacket : IPacket
    {
        public const uint Opcode = 0x71957628;
        public readonly ushort OperandCount;
        public readonly Vector3[] Coordinates;
        CreateClientPathPacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 5))
                throw new InvalidDataException("Unexpected CreateClientPathPacket header");
            Coordinates = new Vector3[PacketFieldCodec.Count(reader, reader.U32(), 12)];
            for (int i = 0; i < Coordinates.Length; i++)
                Coordinates[i] = PacketFieldCodec.Position(reader);
            PacketFieldCodec.End(reader);
        }

        public static CreateClientPathPacket ReadFrom(ByteReader reader) => new CreateClientPathPacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write(checked((uint)Coordinates.Length));
            foreach (var item in Coordinates)
                PacketFieldCodec.Position(writer, item);
        }
    }
}
