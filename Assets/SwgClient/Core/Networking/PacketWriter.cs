using System;
using System.IO;
using System.Text;

namespace Swg.Client.Core.Networking
{
    /// <summary>Reusable encoding scratch space; the returned payload is owned by the caller.</summary>
    public sealed class PacketWriter : IDisposable
    {
        readonly MemoryStream buffer = new MemoryStream(512);
        readonly BinaryWriter writer;
        bool disposed;
        public PacketWriter()
        {
            writer = new BinaryWriter(buffer, Encoding.UTF8, true);
        }

        public byte[] Encode<T>(in T packet)
            where T : struct, IPacket
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(PacketWriter));
            buffer.Position = 0;
            buffer.SetLength(0);
            try
            {
                packet.WriteTo(writer);
                writer.Flush();
                return buffer.ToArray();
            }
            finally
            {
                Array.Clear(buffer.GetBuffer(), 0, checked((int)buffer.Length));
                buffer.Position = 0;
                buffer.SetLength(0);
            }
        }

        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            Array.Clear(buffer.GetBuffer(), 0, buffer.GetBuffer().Length);
            writer.Dispose();
            buffer.Dispose();
        }
    }
}
