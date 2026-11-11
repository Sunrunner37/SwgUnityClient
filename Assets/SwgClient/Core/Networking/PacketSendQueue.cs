using System;
using System.Collections.Concurrent;

namespace Swg.Client.Core.Networking
{
    internal sealed class PacketSendQueue : IDisposable
    {
        readonly ConcurrentQueue<byte[]> pending = new ConcurrentQueue<byte[]>();
        readonly PacketWriter writer = new PacketWriter();
        readonly object gate = new object ();
        bool disposed;
        public void Send<T>(in T packet)
            where T : struct, IPacket
        {
            lock (gate)
            {
                if (disposed)
                    throw new ObjectDisposedException(nameof(PacketSendQueue));
                pending.Enqueue(writer.Encode(in packet));
            }
        }

        public void Flush(Action<byte[]> send)
        {
            while (pending.TryDequeue(out var payload))
            {
                try
                {
                    send(payload);
                }
                finally
                {
                    Array.Clear(payload, 0, payload.Length);
                }
            }
        }

        public void Dispose()
        {
            lock (gate)
            {
                disposed = true;
                writer.Dispose();
                while (pending.TryDequeue(out var payload))
                    Array.Clear(payload, 0, payload.Length);
            }
        }
    }
}
