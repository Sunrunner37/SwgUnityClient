using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Player
{
    // Core3/MMOCoreORB/src/server/zone/packets/player/BadgesResponseMessage.h:12 (BadgesResponseMessage).
    public readonly struct BadgesResponsePacket : IPacket
    {
        public const uint Opcode = 0x6d89d25b;
        public readonly ushort OperandCount;
        public readonly ulong PlayerSwgId;
        public readonly uint ReportedCount;
        public readonly uint[] Bitmasks;
        public readonly uint[] Reserved;
        BadgesResponsePacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 3))
                throw new InvalidDataException("Unexpected BadgesResponsePacket header");
            PlayerSwgId = reader.U64();
            ReportedCount = reader.U32();
            Bitmasks = new uint[5];
            for (int i = 0; i < Bitmasks.Length; i++)
                Bitmasks[i] = reader.U32();
            Reserved = new uint[11];
            for (int i = 0; i < Reserved.Length; i++)
                Reserved[i] = reader.U32();
            PacketFieldCodec.End(reader);
        }

        public static BadgesResponsePacket ReadFrom(ByteReader reader) => new BadgesResponsePacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write(PlayerSwgId);
            writer.Write(ReportedCount);
            foreach (var item in Bitmasks)
                writer.Write(item);
            foreach (var item in Reserved)
                writer.Write(item);
        }
    }
}
