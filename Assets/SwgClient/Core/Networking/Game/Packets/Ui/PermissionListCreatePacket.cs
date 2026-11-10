using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Ui
{
    // Core3/MMOCoreORB/src/server/zone/packets/ui/PermissionListCreateMessage.h:8 (PermissionListCreateMessage).
    public readonly struct PermissionListCreatePacket : IPacket
    {
        public const uint Opcode = 0x52f364b8;
        public readonly ushort OperandCount;
        public readonly string[] Names;
        public readonly uint Reserved;
        public readonly string CellName;
        PermissionListCreatePacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 4))
                throw new InvalidDataException("Unexpected PermissionListCreatePacket header");
            Names = new string[PacketFieldCodec.Count(reader, reader.U32(), 4)];
            for (int i = 0; i < Names.Length; i++)
                Names[i] = reader.Unicode();
            Reserved = reader.U32();
            CellName = reader.Unicode();
            PacketFieldCodec.End(reader);
        }

        public static PermissionListCreatePacket ReadFrom(ByteReader reader) => new PermissionListCreatePacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write(checked((uint)Names.Length));
            foreach (var item in Names)
                PacketFieldCodec.Unicode(writer, item);
            writer.Write(Reserved);
            PacketFieldCodec.Unicode(writer, CellName);
        }
    }
}
