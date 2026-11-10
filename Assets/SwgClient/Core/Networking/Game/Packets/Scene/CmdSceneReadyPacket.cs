using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Networking.Game.Packets.Scene
{
    public readonly struct CmdSceneReadyPacket : IPacket
    {
        public const ushort OperandCount = 1;
        public const uint Opcode = 0x43fd1c22;
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
        }

        public static CmdSceneReadyPacket ReadFrom(ByteReader reader)
        {
            if (reader.U16() != OperandCount || reader.U32() != Opcode)
                throw new InvalidDataException("Unexpected CmdSceneReadyPacket header");
            return new CmdSceneReadyPacket();
        }
    }
}
