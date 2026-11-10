using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Session
{
    // Core3/MMOCoreORB/src/server/zone/packets/zone/ConnectPlayerResponseMessage.h:14 (ConnectPlayerResponseMessage).
    public readonly struct ConnectPlayerResponsePacket : IPacket
    {
        public const uint Opcode = 0x6137556f;
        public readonly ushort OperandCount;
        public readonly uint Result;
        ConnectPlayerResponsePacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 2))
                throw new InvalidDataException("Unexpected ConnectPlayerResponsePacket header");
            Result = reader.U32();
            PacketFieldCodec.End(reader);
        }

        public static ConnectPlayerResponsePacket ReadFrom(ByteReader reader) => new ConnectPlayerResponsePacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write(Result);
        }
    }
}
