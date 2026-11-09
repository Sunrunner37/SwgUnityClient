using System;
using System.Collections.Generic;
using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Networking.Login.Packets
{
    public readonly struct EnumerateCharacterIdPacket : IPacket
    {
        public const ushort OperandCount = 2;
        public const uint Opcode = 0x65ea4574;
        public readonly IReadOnlyList<CharacterEntry> Characters;
        public EnumerateCharacterIdPacket(IReadOnlyList<CharacterEntry> characters)
        {
            Characters = characters ?? throw new ArgumentNullException(nameof(characters));
        }

        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            Wire.Count(writer, Characters.Count);
            for (int i = 0; i < Characters.Count; i++)
            {
                var c = Characters[i];
                Wire.Unicode(writer, c.Name);
                writer.Write(c.Race);
                writer.Write(c.ObjectId);
                writer.Write(c.GalaxyId);
                writer.Write(c.ServerStatus);
            }
        }

        public static EnumerateCharacterIdPacket ReadFrom(ByteReader reader)
        {
            if (reader.U16() != OperandCount || reader.U32() != Opcode)
                throw new InvalidDataException("Unexpected login packet header");
            var characters = new CharacterEntry[reader.Count()];
            for (int i = 0; i < characters.Length; i++)
                characters[i] = new CharacterEntry(reader.Unicode(), reader.U32(), reader.U64(), reader.U32(), reader.U32());
            return new EnumerateCharacterIdPacket(characters);
        }
    }
}
