using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Ui
{
    // Core3/MMOCoreORB/src/server/zone/packets/ui/SuiUpdatePageMessage.h:7 (SuiUpdatePageMessage).
    public readonly struct SuiUpdatePagePacket : IPacket
    {
        public const uint Opcode = 0x5f3342f6;
        public readonly ushort OperandCount;
        public readonly SuiPageData Page;
        SuiUpdatePagePacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 2))
                throw new InvalidDataException("Unexpected SuiUpdatePagePacket header");
            Page = SuiPageData.ReadFrom(reader);
            PacketFieldCodec.End(reader);
        }

        public static SuiUpdatePagePacket ReadFrom(ByteReader reader) => new SuiUpdatePagePacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            Page.WriteTo(writer);
        }
    }
}
