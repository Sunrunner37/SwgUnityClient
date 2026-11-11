using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Resource
{
    // Core3/MMOCoreORB/src/server/zone/packets/resource/ResourceListForSurveyMessage.h:10 (ResourceListForSurveyMessage).
    public readonly struct ResourceListForSurveyPacket : IPacket
    {
        public const uint Opcode = 0x8a64b1d5;
        public readonly ushort OperandCount;
        public readonly SurveyResource[] Resources;
        public readonly string ResourceType;
        public readonly ulong PlayerSwgId;
        ResourceListForSurveyPacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 4))
                throw new InvalidDataException("Unexpected ResourceListForSurveyPacket header");
            Resources = new SurveyResource[PacketFieldCodec.Count(reader, reader.U32(), 12)];
            for (int i = 0; i < Resources.Length; i++)
                Resources[i] = SurveyResource.ReadFrom(reader);
            ResourceType = reader.Ascii();
            PlayerSwgId = reader.U64();
            PacketFieldCodec.End(reader);
        }

        public static ResourceListForSurveyPacket ReadFrom(ByteReader reader) => new ResourceListForSurveyPacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write(checked((uint)Resources.Length));
            foreach (var item in Resources)
                item.WriteTo(writer);
            PacketFieldCodec.Ascii(writer, ResourceType);
            writer.Write(PlayerSwgId);
        }
    }
}
