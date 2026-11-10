using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Scene
{
    // Core3/MMOCoreORB/src/server/zone/packets/scene/GameSceneChangedMessage.h:10 (GameSceneChangedMessage).
    public readonly struct GameSceneChangedPacket : IPacket
    {
        public const uint Opcode = 0xbbadaeb9;
        public readonly ushort OperandCount;
        GameSceneChangedPacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 1))
                throw new InvalidDataException("Unexpected GameSceneChangedPacket header");
            PacketFieldCodec.End(reader);
        }

        public static GameSceneChangedPacket ReadFrom(ByteReader reader) => new GameSceneChangedPacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
        }
    }
}
