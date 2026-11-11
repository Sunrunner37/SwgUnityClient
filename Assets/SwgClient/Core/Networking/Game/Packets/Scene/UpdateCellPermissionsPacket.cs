using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Scene
{
    // Core3/MMOCoreORB/src/server/zone/packets/cell/UpdateCellPermissionsMessage.h:10 (UpdateCellPermissionsMessage).
    public readonly struct UpdateCellPermissionsPacket : IPacket
    {
        public const uint Opcode = 0xf612499c;
        public readonly ushort OperandCount;
        public readonly byte AllowEntry;
        public readonly ulong CellSwgId;
        UpdateCellPermissionsPacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 3))
                throw new InvalidDataException("Unexpected UpdateCellPermissionsPacket header");
            AllowEntry = reader.Byte();
            CellSwgId = reader.U64();
            PacketFieldCodec.End(reader);
        }

        public static UpdateCellPermissionsPacket ReadFrom(ByteReader reader) => new UpdateCellPermissionsPacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write(AllowEntry);
            writer.Write(CellSwgId);
        }
    }
}
