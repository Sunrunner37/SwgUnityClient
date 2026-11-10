using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Scene
{
    // Core3/MMOCoreORB/src/server/zone/packets/scene/AttributeListMessage.h:17 (AttributeListMessage).
    public readonly struct AttributeListPacket : IPacket
    {
        public const uint Opcode = 0xf3f12f2a;
        public readonly ushort OperandCount;
        public readonly ulong ObjectId;
        public readonly AttributeValue[] Attributes;
        AttributeListPacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 4))
                throw new InvalidDataException("Unexpected AttributeListPacket header");
            ObjectId = reader.U64();
            Attributes = new AttributeValue[PacketFieldCodec.Count(reader, reader.U32(), 6)];
            for (int i = 0; i < Attributes.Length; i++)
                Attributes[i] = AttributeValue.ReadFrom(reader);
            PacketFieldCodec.End(reader);
        }

        public static AttributeListPacket ReadFrom(ByteReader reader) => new AttributeListPacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write(ObjectId);
            writer.Write(checked((uint)Attributes.Length));
            foreach (var item in Attributes)
                item.WriteTo(writer);
        }
    }
}
