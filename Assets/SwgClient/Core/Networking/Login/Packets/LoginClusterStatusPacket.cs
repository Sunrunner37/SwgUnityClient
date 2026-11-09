using System;
using System.Collections.Generic;
using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Networking.Login.Packets
{
    public readonly struct LoginClusterStatusPacket : IPacket
    {
        public const ushort OperandCount = 3;
        public const uint Opcode = 0x3436aeb6;
        public const uint OnlineStatus = 2;
        public readonly IReadOnlyList<ClusterStatusEntry> Clusters;
        public LoginClusterStatusPacket(IReadOnlyList<ClusterStatusEntry> clusters)
        {
            Clusters = clusters ?? throw new ArgumentNullException(nameof(clusters));
        }

        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(OperandCount);
            writer.Write(Opcode);
            Wire.Count(writer, Clusters.Count);
            for (int i = 0; i < Clusters.Count; i++)
            {
                var c = Clusters[i];
                writer.Write(c.Id);
                Wire.Ascii(writer, c.Address);
                writer.Write(c.Port);
                writer.Write(c.PingPort);
                writer.Write(c.Population);
                writer.Write(c.Reserved1);
                writer.Write(c.Reserved2);
                writer.Write(c.Reserved3);
                writer.Write(c.Status);
                writer.Write(c.ReservedByte);
            }
        }

        public static LoginClusterStatusPacket ReadFrom(ByteReader reader)
        {
            if (reader.U16() != OperandCount || reader.U32() != Opcode)
                throw new InvalidDataException("Unexpected login packet header");
            var clusters = new ClusterStatusEntry[reader.Count()];
            for (int i = 0; i < clusters.Length; i++)
            {
                uint id = reader.U32();
                string address = reader.Ascii();
                ushort port = reader.U16();
                ushort pingPort = reader.U16();
                uint population = reader.U32();
                uint reserved1 = reader.U32();
                uint reserved2 = reader.U32();
                uint reserved3 = reader.U32();
                uint status = reader.U32();
                byte reservedByte = reader.Byte();
                clusters[i] = new ClusterStatusEntry(id, address, port, pingPort, status, population, reserved1, reserved2, reserved3, reservedByte);
            }

            return new LoginClusterStatusPacket(clusters);
        }
    }
}
