using System;
using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking;
using Swg.Client.Core.Networking.Game;

namespace Swg.Client.Core.Networking.Game.Packets.Scene
{
    public readonly struct SceneDestroyObjectPacket : IPacket
    {
        public const ushort OperandCount = 3;
        public const uint Opcode = 0x4d45d504;
        public readonly ulong SwgId;
        public readonly byte Flags;
        public SceneDestroyObjectPacket(ulong swgId, byte flags = 0)
        {
            SwgId = swgId;
            Flags = flags;
        }

        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write(SwgId);
            writer.Write(Flags);
        }

        public static SceneDestroyObjectPacket ReadFrom(ByteReader reader)
        {
            if (reader.U16() != OperandCount || reader.U32() != Opcode)
                throw new InvalidDataException("Unexpected SceneDestroyObjectPacket header");
            return new SceneDestroyObjectPacket(reader.U64(), reader.Byte());
        }
    }
}
