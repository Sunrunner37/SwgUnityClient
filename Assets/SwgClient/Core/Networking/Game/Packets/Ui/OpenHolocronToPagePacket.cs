using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Ui
{
    // Core3/MMOCoreORB/src/server/zone/packets/ui/OpenHolocronToPageMessage.h:11 (OpenHolocronToPageMessage).
    public readonly struct OpenHolocronToPagePacket : IPacket
    {
        public const uint Opcode = 0x7cb65021;
        public readonly ushort OperandCount;
        public readonly string Page;
        public readonly ushort Reserved;
        OpenHolocronToPagePacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 9))
                throw new InvalidDataException("Unexpected OpenHolocronToPagePacket header");
            Page = reader.Ascii();
            Reserved = reader.U16();
            PacketFieldCodec.End(reader);
        }

        public static OpenHolocronToPagePacket ReadFrom(ByteReader reader) => new OpenHolocronToPagePacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            PacketFieldCodec.Ascii(writer, Page);
            writer.Write(Reserved);
        }
    }
}
