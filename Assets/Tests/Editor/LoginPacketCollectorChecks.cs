using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Swg.Client.Core.Networking;
using Swg.Client.Core.Networking.Login;
using Swg.Client.Core.Networking.Login.Packets;

namespace Swg.Client.Tests.Editor
{
    static class LoginPacketCollectorChecks
    {
        public static void Run()
        {
            var packets = RequiredPackets();
            foreach (var order in Orders(new[] { 0, 1, 2, 3 }))
            {
                var collector = new LoginPacketCollector();
                for (int i = 0; i < order.Length; i++)
                {
                    collector.Accept(packets[order[i]]);
                    Check(collector.Finished == (i == order.Length - 1), "Required packet completeness");
                }

                var result = collector.Result;
                Check(result.AccountId == 7 && result.StationId == 8 && result.Username == "fixture", "Token identity");
                Check(result.Token.SequenceEqual(new byte[] { 11, 22 }), "Token bytes");
                Check(result.Galaxies.Single().Name == "Fixture Galaxy", "Joined cluster status");
                Check(result.Characters.Count == 0, "Empty character enumeration completes login");
            }

            var duplicate = new LoginPacketCollector();
            duplicate.Accept(new byte[] { 0, 0, 0x11, 0x22, 0x33, 0x44 });
            duplicate.Accept(packets[0]);
            duplicate.Accept(packets[0]);
            Check(!duplicate.Finished, "Duplicates and unknown opcodes do not satisfy required packets");
            var error = Reject<InvalidOperationException>(() => duplicate.Accept(Encode(new ErrorPacket("Wrong Password", "Rejected"))));
            Check(error.Message == "Wrong Password: Rejected", "Server rejection propagates");
            Reject<InvalidDataException>(() => duplicate.Accept(new byte[] { 2, 0, 0x74, 0x45, 0xea, 0x65, 255, 255, 255, 255 }));
            Check(!duplicate.Finished, "Malformed packet does not complete collection");
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
