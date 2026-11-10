using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Effects
{
    // Core3/MMOCoreORB/src/server/zone/packets/scene/PlayClientEventObjectMessage.h:15 (PlayClientEventObjectMessage).
    public readonly struct PlayClientEventObjectPacket : IPacket
    {
        public const uint Opcode = 0xaf83c3f2;
        public readonly ushort OperandCount;
        public readonly string File;
        public readonly string Auxiliary;
        public readonly ulong ObjectId;
        PlayClientEventObjectPacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 5))
                throw new InvalidDataException("Unexpected PlayClientEventObjectPacket header");
            File = reader.Ascii();
            Auxiliary = reader.Ascii();
            ObjectId = reader.U64();
            PacketFieldCodec.End(reader);
        }

        public static PlayClientEventObjectPacket ReadFrom(ByteReader reader) => new PlayClientEventObjectPacket(reader);
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
