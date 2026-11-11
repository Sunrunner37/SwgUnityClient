using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.CharacterCreation
{
    // Core3/MMOCoreORB/src/server/zone/packets/charcreation/ClientCreateCharacterSuccess.h:13 (ClientCreateCharacterSuccess).
    public readonly struct ClientCreateCharacterSuccessPacket : IPacket
    {
        public const uint Opcode = 0x1db575cc;
        public readonly ushort OperandCount;
        public readonly ulong SwgId;
        ClientCreateCharacterSuccessPacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 2))
                throw new InvalidDataException("Unexpected ClientCreateCharacterSuccessPacket header");
            SwgId = reader.U64();
            PacketFieldCodec.End(reader);
        }

        public static ClientCreateCharacterSuccessPacket ReadFrom(ByteReader reader) => new ClientCreateCharacterSuccessPacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write(SwgId);
        }
    }
}
