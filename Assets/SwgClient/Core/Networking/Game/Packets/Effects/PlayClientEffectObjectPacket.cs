using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Effects
{
    // Core3/MMOCoreORB/src/server/zone/packets/object/PlayClientEffectObjectMessage.h:12 (PlayClientEffectObjectMessage).
    public readonly struct PlayClientEffectObjectPacket : IPacket
    {
        public const uint Opcode = 0x8855434a;
        public readonly ushort OperandCount;
        public readonly string File;
        public readonly string Auxiliary;
        public readonly ulong ObjectId;
        PlayClientEffectObjectPacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 5))
                throw new InvalidDataException("Unexpected PlayClientEffectObjectPacket header");
            File = reader.Ascii();
            Auxiliary = reader.Ascii();
            ObjectId = reader.U64();
            PacketFieldCodec.End(reader);
        }

        public static PlayClientEffectObjectPacket ReadFrom(ByteReader reader) => new PlayClientEffectObjectPacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            PacketFieldCodec.Ascii(writer, File);
            PacketFieldCodec.Ascii(writer, Auxiliary);
            writer.Write(ObjectId);
        }
    }
}
