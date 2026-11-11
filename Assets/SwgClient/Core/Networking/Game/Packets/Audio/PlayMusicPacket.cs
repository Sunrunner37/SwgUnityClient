using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Audio
{
    // Core3/MMOCoreORB/src/server/zone/packets/player/PlayMusicMessage.h:10 (PlayMusicMessage).
    public readonly struct PlayMusicPacket : IPacket
    {
        public const uint Opcode = 0x4270d8a;
        public readonly ushort OperandCount;
        public readonly string SoundFile;
        public readonly ulong TargetSwgId;
        public readonly int UnknownInt;
        public readonly byte UnknownFlag;
        PlayMusicPacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 5))
                throw new InvalidDataException("Unexpected PlayMusicPacket header");
            SoundFile = reader.Ascii();
            TargetSwgId = reader.U64();
            UnknownInt = unchecked((int)reader.U32());
            UnknownFlag = reader.Byte();
            PacketFieldCodec.End(reader);
        }

        public static PlayMusicPacket ReadFrom(ByteReader reader) => new PlayMusicPacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            PacketFieldCodec.Ascii(writer, SoundFile);
            writer.Write(TargetSwgId);
            writer.Write(UnknownInt);
            writer.Write(UnknownFlag);
        }
    }
}
