using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Swg.Client.Core.Networking;
using Swg.Client.Core.Networking.Game.Packets.Scene;

namespace Swg.Client.Tests.Editor
{
    public sealed class PacketSendQueueChecks
    {
        readonly struct PayloadPacket : IPacket
        {
            readonly byte[] payload;
            public PayloadPacket(params byte[] payload) => this.payload = payload;
            public void WriteTo(BinaryWriter writer) => writer.Write(payload);
        }

        [Test]
        public void QueueCapturesPacketsAtSendTimePreservesOrderAndClearsSentBuffers()
        {
            using var queue = new PacketSendQueue();
            var original = new byte[]
            {
                1,
                2,
                3
            };
            queue.Send(new PayloadPacket(original));
            queue.Send(new CmdSceneReadyPacket());
            original[0] = 99;
            var sent = new List<byte[]>();
            var buffers = new List<byte[]>();
            queue.Flush(bytes =>
            {
                buffers.Add(bytes);
                sent.Add(bytes.ToArray());
            });
            Assert.That(sent[0], Is.EqualTo(new byte[] { 1, 2, 3 }));
            using var writer = new PacketWriter();
            Assert.That(sent[1], Is.EqualTo(writer.Encode(new CmdSceneReadyPacket())));
            Assert.That(buffers.All(bytes => bytes.All(value => value == 0)), Is.True);
        }

        [Test]
        public void MultipleProducersCannotCorruptSerializationOrReorderTheirOwnPackets()
        {
            using var queue = new PacketSendQueue();
            Task.WaitAll(Enumerable.Range(0, 4).Select(producer => Task.Run(() =>
            {
                for (int i = 0; i < 100; i++)
                    queue.Send(new PayloadPacket((byte)producer, (byte)i));
            })).ToArray());
            var next = new int[4];
            queue.Flush(bytes =>
            {
                Assert.That(bytes[1], Is.EqualTo(next[bytes[0]]));
                next[bytes[0]]++;
            });
            Assert.That(next, Is.EqualTo(new[] { 100, 100, 100, 100 }));
        }

        [Test]
        public void FailedTransmissionStillClearsItsPayload()
        {
            using var queue = new PacketSendQueue();
            queue.Send(new PayloadPacket(1, 2, 3));
            byte[] buffer = null;
            Assert.Throws<IOException>(() => queue.Flush(bytes =>
            {
                buffer = bytes;
                throw new IOException();
            }));
            Assert.That(buffer, Is.EqualTo(new byte[3]));
        }
    }
}
