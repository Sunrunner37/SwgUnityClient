using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Ui
{
    // Core3/MMOCoreORB/src/server/zone/packets/ui/CloseHolocronMessage.h:13 (CloseHolocronMessage).
    public readonly struct CloseHolocronPacket : IPacket
    {
        public const uint Opcode = 0xc0938a9d;
        public readonly ushort OperandCount;
        CloseHolocronPacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 1))
                throw new InvalidDataException("Unexpected CloseHolocronPacket header");
            PacketFieldCodec.End(reader);
        }

        public static CloseHolocronPacket ReadFrom(ByteReader reader) => new CloseHolocronPacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
        }
    }
}
