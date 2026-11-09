using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Swg.Client.Core.Networking;
using Swg.Client.Core.Networking.Login;
using Swg.Client.Core.Networking.Login.Packets;

namespace Swg.Client.Tests
{
    static class LoginPacketCollectorChecks
    {
        public static void Run()
        {
            var packets = RequiredPackets();
            foreach (var order in Orders(new[] { 0, 1, 2, 3 }))
            {
                using var collector = new LoginPacketCollector();
                for (int i = 0; i < order.Length; i++)
                {
                    collector.Accept(packets[order[i]]);
                    bool ready = collector.TryGetResult(out var result);
                    Check(ready == (i == order.Length - 1), "Incorrect completeness for packet order " + string.Join(",", order));
                    if (!ready)
                        continue;
                    Check(result.AccountId == 7 && result.StationId == 8 && result.Username == "fixture", "Token identity fields");
                    Check(result.Token.SequenceEqual(new byte[] { 11, 22 }), "Transferred token");
                    Check(result.Galaxies.Count == 1 && result.Galaxies[0].Name == "Fixture Galaxy", "Cluster names/status were not joined");
                    Check(result.Characters.Count == 0, "Empty character packet must complete collection");
                    Check(!collector.TryGetResult(out _), "Result was returned twice");
                    collector.Dispose();
                    Check(result.Token.SequenceEqual(new byte[] { 11, 22 }), "Collector retained ownership of transferred token");
                    Reject<InvalidOperationException>(() => collector.Accept(packets[0]));
                    Array.Clear(result.Token, 0, result.Token.Length);
                }
            }

            using (var collector = new LoginPacketCollector())
            {
                collector.Accept(new byte[] { 0, 0, 0x11, 0x22, 0x33, 0x44 });
                collector.Accept(packets[0]);
                collector.Accept(packets[1]);
                collector.Accept(packets[2]);
                collector.Accept(packets[2]);
                Check(!collector.TryGetResult(out _), "Unknown/duplicate packets counted as missing required packets");
                collector.Accept(Encode(new LoginClientTokenPacket(new byte[] { 99 }, 99, 100, "replacement")));
                collector.Accept(Encode(new LoginEnumClusterPacket(Array.Empty<ClusterNameEntry>())));
                collector.Accept(Encode(new EnumerateCharacterIdPacket(new[] { new CharacterEntry("Test", 123, 456, 7) })));
                Check(collector.TryGetResult(out var result), "Required character packet did not complete collection");
                Check(result.AccountId == 99 && result.Token.Single() == 99, "Duplicate token did not replace earlier data");
                Check(result.Galaxies[0].Name == "Galaxy 7", "Replacement empty names packet did not use the fallback name");
                Check(result.Characters.Single().Name == "Test" && result.Characters[0].ObjectId == 456, "Character result mapping");
                Array.Clear(result.Token, 0, result.Token.Length);
            }

            foreach (int received in new[]
            {
                0,
                1,
                3,
                4
            }

            )
            {
                using var collector = new LoginPacketCollector();
                foreach (var packet in packets.Take(received))
                    collector.Accept(packet);
                var error = Reject<InvalidOperationException>(() => collector.Accept(Encode(new ErrorPacket("Wrong Password", "Rejected"))));
                Check(error.Message == "Wrong Password: Rejected", "Server error was not propagated immediately");
                Check(!collector.TryGetResult(out _), "Failed collection returned a result");
            }

            using (var collector = new LoginPacketCollector())
            {
                collector.Accept(packets[0]);
                Reject<InvalidDataException>(() => collector.Accept(new byte[] { 2, 0, 0x74, 0x45, 0xea, 0x65, 255, 255, 255, 255 }));
                Check(!collector.TryGetResult(out _), "Malformed packet left a usable partial result");
            }
        }

        static byte[][] RequiredPackets() => new[]
        {
            Encode(new LoginClientTokenPacket(new byte[] { 11, 22 }, 7, 8, "fixture")),
            Encode(new LoginEnumClusterPacket(new[] { new ClusterNameEntry(7, "Fixture Galaxy") })),
            Encode(new LoginClusterStatusPacket(new[] { new ClusterStatusEntry(7, "127.0.0.1", 44463, 44464) })),
            Encode(new EnumerateCharacterIdPacket(Array.Empty<CharacterEntry>()))
        };
        static byte[] Encode<T>(in T packet)
            where T : struct, IPacket
        {
            using var writer = new PacketWriter();
            return writer.Encode(in packet);
        }

        static IEnumerable<int[]> Orders(int[] remaining)
        {
            if (remaining.Length == 0)
            {
                yield return Array.Empty<int>();
                yield break;
            }

            foreach (int head in remaining)
                foreach (var tail in Orders(remaining.Where(value => value != head).ToArray()))
                    yield return new[]
                    {
                        head
                    }.Concat(tail).ToArray();
        }

        static void Check(bool value, string message)
        {
            if (!value)
                throw new Exception(message);
        }

        static T Reject<T>(Action action)
            where T : Exception
        {
            try
            {
                action();
            }
            catch (T error)
            {
                return error;
            }

            throw new Exception("Expected " + typeof(T).Name);
        }
    }
}
