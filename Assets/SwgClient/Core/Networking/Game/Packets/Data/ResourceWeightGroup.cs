using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Networking.Game.Packets.Data
{
    public readonly struct ResourceWeightGroup
    {
        public readonly byte[] Weights;
        ResourceWeightGroup(ByteReader reader)
        {
            this = default;
            Weights = new byte[PacketFieldCodec.Count(reader, reader.Byte(), 1)];
            for (int i = 0; i < Weights.Length; i++)
                Weights[i] = reader.Byte();
        }

        public static ResourceWeightGroup ReadFrom(ByteReader reader) => new ResourceWeightGroup(reader);
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(checked((byte)Weights.Length));
            foreach (var item in Weights)
                writer.Write(item);
        }
    }
}
