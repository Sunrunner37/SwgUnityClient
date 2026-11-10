using System.Collections.Generic;
using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Support
{
    // Core3/MMOCoreORB/src/server/zone/packets/ui/GetTicketsResponseMessage.h:7 (GetTicketsResponseMessage).
    public readonly struct GetTicketsResponsePacket : IPacket
    {
        public const uint Opcode = 0xbb567f98;
        public readonly ushort OperandCount;
        public readonly uint[] HeaderWords;
        public readonly TicketData[] Tickets;
        GetTicketsResponsePacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 3))
                throw new InvalidDataException("Unexpected GetTicketsResponsePacket header");
            // Core3 patches the legacy count at an unaligned offset; records end at the packet boundary.
            HeaderWords = new uint[3];
            for (int i = 0; i < HeaderWords.Length; i++)
                HeaderWords[i] = reader.U32();
            var tickets = new List<TicketData>();
            while (reader.Remaining > 0)
                tickets.Add(TicketData.ReadFrom(reader));
            Tickets = tickets.ToArray();
            PacketFieldCodec.End(reader);
        }

        public static GetTicketsResponsePacket ReadFrom(ByteReader reader) => new GetTicketsResponsePacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            foreach (var item in HeaderWords)
                writer.Write(item);
            foreach (var item in Tickets)
                item.WriteTo(writer);
        }
    }
}
