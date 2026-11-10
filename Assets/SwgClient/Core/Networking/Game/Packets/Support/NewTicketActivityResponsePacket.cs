using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Support
{
    // Core3/MMOCoreORB/src/server/zone/packets/ui/NewTicketActivityResponseMessage.h:11 (NewTicketActivityResponseMessage).
    public readonly struct NewTicketActivityResponsePacket : IPacket
    {
        public const uint Opcode = 0x6ea42d80;
        public readonly ushort OperandCount;
        public readonly byte Flag;
        public readonly uint TicketId;
        NewTicketActivityResponsePacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 3))
                throw new InvalidDataException("Unexpected NewTicketActivityResponsePacket header");
            Flag = reader.Byte();
            TicketId = reader.U32();
            PacketFieldCodec.End(reader);
        }

        public static NewTicketActivityResponsePacket ReadFrom(ByteReader reader) => new NewTicketActivityResponsePacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write(Flag);
            writer.Write(TicketId);
        }
    }
}
