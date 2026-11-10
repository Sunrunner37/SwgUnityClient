using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Player
{
    // Core3/MMOCoreORB/src/server/zone/packets/player/EnterTicketPurchaseModeMessage.h:11 (EnterTicketPurchaseModeMessage).
    public readonly struct EnterTicketPurchaseModePacket : IPacket
    {
        public const uint Opcode = 0x904dae1a;
        public readonly ushort OperandCount;
        public readonly string Planet;
        public readonly string PointName;
        public readonly byte Reserved;
        EnterTicketPurchaseModePacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 4))
                throw new InvalidDataException("Unexpected EnterTicketPurchaseModePacket header");
            Planet = reader.Ascii();
            PointName = reader.Ascii();
            Reserved = reader.Byte();
            PacketFieldCodec.End(reader);
        }

        public static EnterTicketPurchaseModePacket ReadFrom(ByteReader reader) => new EnterTicketPurchaseModePacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            PacketFieldCodec.Ascii(writer, Planet);
            PacketFieldCodec.Ascii(writer, PointName);
            writer.Write(Reserved);
        }
    }
}
