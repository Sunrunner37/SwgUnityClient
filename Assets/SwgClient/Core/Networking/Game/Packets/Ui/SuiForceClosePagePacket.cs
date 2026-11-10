using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Ui
{
    // Core3/MMOCoreORB/src/server/zone/packets/ui/SuiForceClosePage.h:8 (SuiForceClosePage).
    public readonly struct SuiForceClosePagePacket : IPacket
    {
        public const uint Opcode = 0x990b5de0;
        public readonly ushort OperandCount;
        public readonly uint PageId;
        SuiForceClosePagePacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 2))
                throw new InvalidDataException("Unexpected SuiForceClosePagePacket header");
            PageId = reader.U32();
            PacketFieldCodec.End(reader);
        }

        public static SuiForceClosePagePacket ReadFrom(ByteReader reader) => new SuiForceClosePagePacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write(PageId);
        }
    }
}
