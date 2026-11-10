using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Support
{
    // Core3/MMOCoreORB/src/server/zone/packets/ui/AppendCommentsResponseMessage.h:10 (AppendCommentsResponseMessage).
    public readonly struct AppendCommentsResponsePacket : IPacket
    {
        public const uint Opcode = 0xa04a3eca;
        public readonly ushort OperandCount;
        public readonly uint Result;
        public readonly uint TicketId;
        AppendCommentsResponsePacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 3))
                throw new InvalidDataException("Unexpected AppendCommentsResponsePacket header");
            Result = reader.U32();
            TicketId = reader.U32();
            PacketFieldCodec.End(reader);
        }

        public static AppendCommentsResponsePacket ReadFrom(ByteReader reader) => new AppendCommentsResponsePacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write(Result);
            writer.Write(TicketId);
        }
    }
}
