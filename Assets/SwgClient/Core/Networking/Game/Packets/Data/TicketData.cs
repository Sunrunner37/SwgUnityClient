using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Networking.Game.Packets.Data
{
    public readonly struct TicketData
    {
        public readonly uint Reserved1;
        public readonly uint Reserved2;
        public readonly string Name;
        public readonly string Body;
        public readonly string ReservedText;
        public readonly uint TicketId;
        public readonly ulong Timestamp;
        public readonly byte Unread;
        public readonly byte Closed;
        TicketData(ByteReader reader)
        {
            this = default;
            Reserved1 = reader.U32();
            Reserved2 = reader.U32();
            Name = reader.Ascii();
            Body = reader.Unicode();
            ReservedText = reader.Ascii();
            TicketId = reader.U32();
            Timestamp = reader.U64();
            Unread = reader.Byte();
            Closed = reader.Byte();
        }

        public static TicketData ReadFrom(ByteReader reader) => new TicketData(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(Reserved1);
            writer.Write(Reserved2);
            PacketFieldCodec.Ascii(writer, Name);
            PacketFieldCodec.Unicode(writer, Body);
            PacketFieldCodec.Ascii(writer, ReservedText);
            writer.Write(TicketId);
            writer.Write(Timestamp);
            writer.Write(Unread);
            writer.Write(Closed);
        }
    }
}
