using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.CharacterCreation
{
    // Core3/MMOCoreORB/src/server/zone/packets/charcreation/ClientRandomNameResponse.h:10 (ClientRandomNameResponse).
    public readonly struct ClientRandomNameResponsePacket : IPacket
    {
        public const uint Opcode = 0xe85fb868;
        public readonly ushort OperandCount;
        public readonly string RaceTemplate;
        public readonly string Name;
        public readonly string StringFile;
        public readonly uint Reserved;
        public readonly string ApprovalKey;
        ClientRandomNameResponsePacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 4))
                throw new InvalidDataException("Unexpected ClientRandomNameResponsePacket header");
            RaceTemplate = reader.Ascii();
            Name = reader.Unicode();
            StringFile = reader.Ascii();
            Reserved = reader.U32();
            ApprovalKey = reader.Ascii();
            PacketFieldCodec.End(reader);
        }

        public static ClientRandomNameResponsePacket ReadFrom(ByteReader reader) => new ClientRandomNameResponsePacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            PacketFieldCodec.Ascii(writer, RaceTemplate);
            PacketFieldCodec.Unicode(writer, Name);
            PacketFieldCodec.Ascii(writer, StringFile);
            writer.Write(Reserved);
            PacketFieldCodec.Ascii(writer, ApprovalKey);
        }
    }
}
