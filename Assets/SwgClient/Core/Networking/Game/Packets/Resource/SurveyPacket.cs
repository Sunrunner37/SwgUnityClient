using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;

namespace Swg.Client.Core.Networking.Game.Packets.Resource
{
    // Core3/MMOCoreORB/src/server/zone/packets/resource/SurveyMessage.h:10 (SurveyMessage).
    public readonly struct SurveyPacket : IPacket
    {
        public const uint Opcode = 0x877f79ac;
        public readonly ushort OperandCount;
        public readonly SurveyPoint[] Points;
        SurveyPacket(ByteReader reader)
        {
            this = default;
            OperandCount = reader.U16();
            if (reader.U32() != Opcode || (OperandCount != 2))
                throw new InvalidDataException("Unexpected SurveyPacket header");
            Points = new SurveyPoint[PacketFieldCodec.Count(reader, reader.U32(), 16)];
            for (int i = 0; i < Points.Length; i++)
                Points[i] = SurveyPoint.ReadFrom(reader);
            PacketFieldCodec.End(reader);
        }

        public static SurveyPacket ReadFrom(ByteReader reader) => new SurveyPacket(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            writer.Write(checked((uint)Points.Length));
            foreach (var item in Points)
                item.WriteTo(writer);
        }
    }
}
