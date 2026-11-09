using System;
using System.Collections.Generic;
using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Networking.Login.Packets
{
    public readonly struct LoginEnumClusterPacket : IPacket
    {
        public const ushort OperandCount = 2;
        public const uint Opcode = 0xc11c63b9;
        public readonly IReadOnlyList<ClusterNameEntry> Clusters;
        public readonly uint Reserved;
        public LoginEnumClusterPacket(IReadOnlyList<ClusterNameEntry> clusters, uint reserved = 8)
        {
            Clusters = clusters ?? throw new ArgumentNullException(nameof(clusters));
            Reserved = reserved;
        }

        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            Wire.Count(writer, Clusters.Count);
            for (int i = 0; i < Clusters.Count; i++)
            {
                var cluster = Clusters[i];
                writer.Write(cluster.Id);
                Wire.Ascii(writer, cluster.Name);
                writer.Write(cluster.Reserved);
            }

            writer.Write(Reserved);
        }

        public static LoginEnumClusterPacket ReadFrom(ByteReader reader)
        {
            if (reader.U16() != OperandCount || reader.U32() != Opcode)
                throw new InvalidDataException("Unexpected login packet header");
            var clusters = new ClusterNameEntry[reader.Count()];
            for (int i = 0; i < clusters.Length; i++)
                clusters[i] = new ClusterNameEntry(reader.U32(), reader.Ascii(), reader.U32());
            return new LoginEnumClusterPacket(clusters, reader.U32());
        }
    }
}
