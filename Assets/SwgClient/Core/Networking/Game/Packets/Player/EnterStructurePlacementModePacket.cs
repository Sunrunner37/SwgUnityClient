using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Player
{
    // Core3/MMOCoreORB/src/server/zone/packets/player/EnterStructurePlacementModeMessage.h:10 (EnterStructurePlacementModeMessage).
    public readonly struct EnterStructurePlacementModePacket : IPacket
    {
        public const uint Opcode = 0xe8a54dc1;
        public readonly ushort OperandCount;
        public readonly ulong DeedSwgId;
        public readonly string TemplatePath;
        EnterStructurePlacementModePacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 3))
                throw new InvalidDataException("Unexpected EnterStructurePlacementModePacket header");
            DeedSwgId = reader.U64();
            TemplatePath = reader.Ascii();
            PacketFieldCodec.End(reader);
        }

        public static EnterStructurePlacementModePacket ReadFrom(ByteReader reader) => new EnterStructurePlacementModePacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write(DeedSwgId);
            PacketFieldCodec.Ascii(writer, TemplatePath);
        }
    }
}
