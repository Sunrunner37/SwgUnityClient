using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Ui
{
    // Core3/MMOCoreORB/src/server/zone/packets/ui/SuiCreatePageMessage.h:17 (SuiCreatePageMessage).
    public readonly struct SuiCreatePagePacket : IPacket
    {
        public const uint Opcode = 0xd44b7259;
        public readonly ushort OperandCount;
        public readonly SuiPageData Page;
        SuiCreatePagePacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 2))
                throw new InvalidDataException("Unexpected SuiCreatePagePacket header");
            Page = SuiPageData.ReadFrom(reader);
            PacketFieldCodec.End(reader);
        }

        public static SuiCreatePagePacket ReadFrom(ByteReader reader) => new SuiCreatePagePacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            Page.WriteTo(writer);
        }
    }
}
