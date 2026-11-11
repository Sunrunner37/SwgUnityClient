using System;
using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking;
using Swg.Client.Core.Networking.Game;

namespace Swg.Client.Core.Networking.Game.Packets.Scene
{
    public readonly struct SceneEndBaselinesPacket : IPacket
    {
        public const ushort OperandCount = 2;
        public const uint Opcode = 0x2c436037;
        public readonly ulong SwgId;
        public SceneEndBaselinesPacket(ulong swgId)
        {
            SwgId = swgId;
        }

        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write(SwgId);
        }

        public static SceneEndBaselinesPacket ReadFrom(ByteReader reader)
        {
            if (reader.U16() != OperandCount || reader.U32() != Opcode)
                throw new InvalidDataException("Unexpected SceneEndBaselinesPacket header");
            return new SceneEndBaselinesPacket(reader.U64());
        }
    }
}
