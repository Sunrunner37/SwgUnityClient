using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Ui
{
    // Core3/MMOCoreORB/src/server/zone/packets/ui/DestroyClientPathMessage.h:11 (DestroyClientPathMessage).
    public readonly struct DestroyClientPathPacket : IPacket
    {
        public const uint Opcode = 0xa75e85eb;
        public readonly ushort OperandCount;
        DestroyClientPathPacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 1))
                throw new InvalidDataException("Unexpected DestroyClientPathPacket header");
            PacketFieldCodec.End(reader);
        }

        public static DestroyClientPathPacket ReadFrom(ByteReader reader) => new DestroyClientPathPacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
        }
    }
}
