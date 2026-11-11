using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Scene
{
    // Core3/MMOCoreORB/src/server/zone/packets/scene/ClientOpenContainerMessage.h.
    public readonly struct ClientOpenContainerPacket : IPacket
    {
        public const uint Opcode = 0xdca57409;
        public readonly ushort OperandCount;
        public readonly ulong SwgId;
        public readonly string SlotName;
        public readonly uint Reserved;
        public readonly ushort Mode;
        public readonly bool UsesSlotName;
        ClientOpenContainerPacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (OperandCount != 4 || reader.U32() != Opcode)
                throw new InvalidDataException("Unexpected ClientOpenContainerPacket header");
            SwgId = reader.U64();
            // The object constructors append a zero uint + mode; the slot constructor appends an ASCII string.
            UsesSlotName = reader.Remaining != 6 || ByteReader.At(reader.Bytes, reader.Position) != 0;
            if (UsesSlotName)
                SlotName = reader.Ascii();
            else
            {
                Reserved = reader.U32();
                Mode = reader.U16();
            }

            PacketFieldCodec.End(reader);
        }

        public static ClientOpenContainerPacket ReadFrom(ByteReader reader) => new ClientOpenContainerPacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write(SwgId);
            if (UsesSlotName)
                PacketFieldCodec.Ascii(writer, SlotName);
            else
            {
                writer.Write(Reserved);
                writer.Write(Mode);
            }
        }
    }
}
