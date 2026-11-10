using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Networking.Soe
{
    internal sealed class SoeTransport : IDisposable
    {
        sealed class Pending
        {
            public ushort Sequence;
            public byte[] Bytes;
            public DateTime Sent;
            public int Retries;
        }

        readonly PacketWriter packetWriter = new PacketWriter();
        readonly UdpClient udp;
        readonly uint seed;
        ushort sendSequence;
        readonly List<Pending> pending = new List<Pending>();
        readonly OrderedChannel ordered = new OrderedChannel();
        DateTime lastPing = DateTime.UtcNow;
        DateTime lastReceive = DateTime.UtcNow;
        bool disposed;
        SoeTransport(UdpClient udp, uint seed)
        {
            this.udp = udp;
            this.seed = seed;
        }

        public static SoeTransport Connect(string host, ushort port, CancellationToken cancel)
        {
            IPAddress ip = Dns.GetHostAddresses(host.Trim('[', ']')).FirstOrDefault() ?? throw new SocketException();
            var socket = new UdpClient(ip.AddressFamily);
            try
            {
                socket.Connect(ip, port);
                socket.Client.ReceiveTimeout = 50;
                uint id = (uint)(Environment.TickCount ^ System.Diagnostics.Process.GetCurrentProcess().Id);
                byte[] request;
                using (var m = new MemoryStream())
                using (var w = new BinaryWriter(m))
                {
                    w.Write(new byte[] { 0, 1, 0, 0, 0, 2 });
                    w.Write(id);
                    NetworkByteOrder.Write32(w, 496);
                    request = m.ToArray();
                }

                DateTime deadline = DateTime.UtcNow.AddSeconds(8);
                DateTime sent = DateTime.MinValue;
                while (true)
                {
                    cancel.ThrowIfCancellationRequested();
                    if (DateTime.UtcNow > deadline)
                        throw new TimeoutException("No UDP session response from " + host + ":" + port);
                    if ((DateTime.UtcNow - sent).TotalMilliseconds > 500)
                    {
                        socket.Send(request, request.Length);
                        sent = DateTime.UtcNow;
                    }

                    try
                    {
                        IPEndPoint peer = null;
                        byte[] b = socket.Receive(ref peer);
                        if (b.Length >= 17 && b[0] == 0 && b[1] == 2 && ByteReader.At(b, 2) == id)
                        {
                            if (b[10] != 2 || b[11] != 1 || b[12] != 4)
                                throw new InvalidDataException("Unsupported server transport settings");
                            var r = new ByteReader(b)
                            {
                                Position = 6
                            };
                            return new SoeTransport(socket, r.Net32());
                        }
                    }
                    catch (SocketException e)when (IsTimeout(e))
                    {
                    }
                }
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        }

        void Send(byte[] plain)
        {
            var b = SoeCodec.Encode(plain, seed);
            udp.Send(b, b.Length);
        }

        public void SendData<T>(in T packet)
            where T : struct, IPacket
        {
            var payload = packetWriter.Encode(in packet);
            try
            {
                SendData(payload);
            }
            finally
            {
                Array.Clear(payload, 0, payload.Length);
            }
        }

        void SendData(byte[] application)
        {
            if (application.Length > 480)
                throw new InvalidDataException("Outgoing message exceeds packet limit");
            ushort seq = sendSequence++;
            var bytes = SoeCodec.Encode(SoeCodec.Data(seq, application), seed);
            udp.Send(bytes, bytes.Length);
            pending.Add(new Pending { Sequence = seq, Bytes = bytes, Sent = DateTime.UtcNow });
        }

        public List<byte[]> Tick()
        {
            foreach (var p in pending)
            {
                if ((DateTime.UtcNow - p.Sent).TotalMilliseconds > 500)
                {
                    if (p.Retries++ >= 12)
                        throw new TimeoutException("Server did not acknowledge a reliable packet");
                    udp.Send(p.Bytes, p.Bytes.Length);
                    p.Sent = DateTime.UtcNow;
                }
            }

            if ((DateTime.UtcNow - lastPing).TotalSeconds > 2)
            {
                using (var m = new MemoryStream())
                using (var w = new BinaryWriter(m))
                {
                    w.Write(new byte[] { 0, 7 });
                    NetworkByteOrder.Write16(w, (ushort)Environment.TickCount);
                    w.Write(new byte[36]);
                    Send(m.ToArray());
                }

                lastPing = DateTime.UtcNow;
            }

            if ((DateTime.UtcNow - lastReceive).TotalSeconds > 45)
                throw new TimeoutException("Server connection timed out");
            try
            {
                IPEndPoint peer = null;
                var b = udp.Receive(ref peer);
                byte[] plain;
                try
                {
                    plain = SoeCodec.Decode(b, seed);
                }
                catch (InvalidDataException)
                {
                    return new List<byte[]>();
                }

                lastReceive = DateTime.UtcNow;
                var result = new List<byte[]>();
                Handle(plain, result, 0);
                return result;
            }
            catch (SocketException e)when (IsTimeout(e))
            {
                return new List<byte[]>();
            }
        }

        void Handle(byte[] b, List<byte[]> messages, int depth)
        {
            if (depth > 8 || b.Length < 2)
                throw new InvalidDataException("Invalid nested UDP packet");
            if (b[0] != 0)
                return;
            switch (b[1])
            {
                case 3:
                    var r = new ByteReader(b)
                    {
                        Position = 2
                    };
                    while (r.Remaining > 0)
                        Handle(r.Take(r.Byte()), messages, depth + 1);
                    break;
                case 9:
                case 13:
                    messages.AddRange(ordered.Accept(b));
                    if (ordered.LastAck.HasValue)
                    {
                        ushort ack = ordered.LastAck.Value;
                        Send(new byte[] { 0, 21, (byte)(ack >> 8), (byte)ack });
                    }

                    break;
                case 21:
                    if (b.Length != 4)
                        throw new InvalidDataException("Invalid acknowledgement");
                    ushort seq = (ushort)(b[2] << 8 | b[3]);
                    foreach (var p in pending.ToArray())
                    {
                        ushort distance = (ushort)(p.Sequence - seq);
                        if (distance == 0 || distance >= 32768)
                        {
                            Array.Clear(p.Bytes, 0, p.Bytes.Length);
                            pending.Remove(p);
                        }
                    }

                    break;
                case 17:
                    foreach (var p in pending)
                    {
                        udp.Send(p.Bytes, p.Bytes.Length);
                        p.Sent = DateTime.UtcNow;
                    }

                    break;
                case 5:
                    throw new IOException("Server disconnected the session");
                case 7:
                    if (b.Length >= 4)
                    {
                        var status = new byte[40];
                        status[1] = 8;
                        status[2] = b[2];
                        status[3] = b[3];
                        Send(status);
                    }

                    break;
            }
        }

        static bool IsTimeout(SocketException e) => e.SocketErrorCode == SocketError.TimedOut || e.SocketErrorCode == SocketError.WouldBlock;
        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            try
            {
                Send(new byte[] { 0, 5 });
            }
            catch (SocketException)
            {
            }
            finally
            {
                foreach (var p in pending)
                    Array.Clear(p.Bytes, 0, p.Bytes.Length);
                packetWriter.Dispose();
                udp.Dispose();
            }
        }
    }
}
