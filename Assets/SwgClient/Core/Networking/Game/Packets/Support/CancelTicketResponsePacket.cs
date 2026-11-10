using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Support
{
    // Core3/MMOCoreORB/src/server/zone/packets/ui/CancelTicketResponseMessage.h:10 (CancelTicketResponseMessage).
    public readonly struct CancelTicketResponsePacket : IPacket
    {
        public const uint Opcode = 0xd6fbf318;
        public readonly ushort OperandCount;
        public readonly uint Result;
        public readonly uint TicketId;
        CancelTicketResponsePacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 3))
                throw new InvalidDataException("Unexpected CancelTicketResponsePacket header");
            Result = reader.U32();
            TicketId = reader.U32();
            PacketFieldCodec.End(reader);
        }

        public static CancelTicketResponsePacket ReadFrom(ByteReader reader) => new CancelTicketResponsePacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write(Result);
            writer.Write(TicketId);
        }
    }
}
