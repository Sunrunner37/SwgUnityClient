using System;
using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking;
using Swg.Client.Core.Networking.Game;

namespace Swg.Client.Core.Networking.Game.Packets.Session
{
    public readonly struct SelectCharacterPacket : IPacket
    {
        public const ushort OperandCount = 2;
        public const uint Opcode = 0xb5098d76;
        public readonly ulong SwgId;
        public SelectCharacterPacket(ulong swgId)
        {
            SwgId = swgId;
        }

        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write(SwgId);
        }

        public static SelectCharacterPacket ReadFrom(ByteReader reader)
        {
            if (reader.U16() != OperandCount || reader.U32() != Opcode)
                throw new InvalidDataException("Unexpected SelectCharacterPacket header");
            return new SelectCharacterPacket(reader.U64());
        }
    }
}
