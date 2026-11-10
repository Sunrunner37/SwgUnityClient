using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.CharacterCreation
{
    // Core3/MMOCoreORB/src/server/zone/packets/charcreation/ClientCreateCharacterFailed.h:13 (ClientCreateCharacterFailed).
    public readonly struct ClientCreateCharacterFailedPacket : IPacket
    {
        public const uint Opcode = 0xdf333c6e;
        public readonly ushort OperandCount;
        public readonly uint Reserved1;
        public readonly string StringFile;
        public readonly uint Reserved2;
        public readonly string ErrorKey;
        ClientCreateCharacterFailedPacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 3))
                throw new InvalidDataException("Unexpected ClientCreateCharacterFailedPacket header");
            Reserved1 = reader.U32();
            StringFile = reader.Ascii();
            Reserved2 = reader.U32();
            ErrorKey = reader.Ascii();
            PacketFieldCodec.End(reader);
        }

        public static ClientCreateCharacterFailedPacket ReadFrom(ByteReader reader) => new ClientCreateCharacterFailedPacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write(Reserved1);
            PacketFieldCodec.Ascii(writer, StringFile);
            writer.Write(Reserved2);
            PacketFieldCodec.Ascii(writer, ErrorKey);
        }
    }
}
