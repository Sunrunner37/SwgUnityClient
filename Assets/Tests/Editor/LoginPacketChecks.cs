using System;
using System.IO;
using System.Linq;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking;
using Swg.Client.Core.Networking.Login.Packets;

namespace Swg.Client.Tests.Editor
{
    static class LoginPacketChecks
    {
        public static void Run()
        {
            Golden(new LoginCredentialsPacket("a", "b", "v"), "0400961f1341010061010062010076", LoginCredentialsPacket.ReadFrom);
            Golden(new ClientIdentificationPacket(new byte[] { 0xaa, 0xbb }, 42, "v"), "0300269289d50000000006000000aabb2a000000010076", ClientIdentificationPacket.ReadFrom);
            Golden(new LoginClientTokenPacket(new byte[] { 0xaa, 0xbb }, 42, 43, "a"), "0400c696b2aa06000000aabb2a0000002b000000010061", LoginClientTokenPacket.ReadFrom);
            Golden(new LoginEnumClusterPacket(new[] { new ClusterNameEntry(7, "g") }), "0200b9631cc10100000007000000010067808fffff08000000", LoginEnumClusterPacket.ReadFrom);
            Golden(new LoginClusterStatusPacket(new[] { new ClusterStatusEntry(7, "h", 0x1234, 0x5678) }), "0300b6ae363401000000070000000100683412785664000000b20c000008000000808fffff0200000000", LoginClusterStatusPacket.ReadFrom);
            Golden(new EnumerateCharacterIdPacket(new[] { new CharacterEntry("Tést", 0x1234, 0x100000001, 7) }), "02007445ea6501000000040000005400e900730074003412000001000000010000000700000001000000", EnumerateCharacterIdPacket.ReadFrom);
            Golden(new ErrorPacket("x", "y", 1), "03001af9abb501007801007901", ErrorPacket.ReadFrom);
            Golden(new ClientPermissionsPacket(true, false), "0400e53007e0010000", ClientPermissionsPacket.ReadFrom);
            var token = TestHex.Decode("0400c696b2aa06000000aabb2a0000002b000000010061");
            Buffer.BlockCopy(BitConverter.GetBytes(5000u), 0, token, 6, sizeof(uint));
            Reject(() => LoginClientTokenPacket.ReadFrom(new ByteReader(token)));
            Reject(() => new ClientIdentificationPacket(Array.Empty<byte>(), 42, "v"));
            using (var writer = new PacketWriter())
            {
                Reject(() => writer.Encode(default(LoginCredentialsPacket)));
                var valid = writer.Encode(new ClientPermissionsPacket(true, false));
                if (!valid.SequenceEqual(TestHex.Decode("0400e53007e0010000")))
                    throw new Exception("Packet writer retained bytes after an encoding failure");
                Reject(() => writer.Encode(new EnumerateCharacterIdPacket(new CharacterEntry[4097])));
            }
        }

        static void Golden<T>(T packet, string hex, Func<ByteReader, T> read)
            where T : struct, IPacket
        {
            var expected = TestHex.Decode(hex);
            using (var writer = new PacketWriter())
            {
                if (!writer.Encode(in packet).SequenceEqual(expected))
                    throw new Exception(typeof(T).Name + " differs from its independent wire fixture");
                var parsed = read(new ByteReader(expected));
                if (!writer.Encode(in parsed).SequenceEqual(expected))
                    throw new Exception(typeof(T).Name + " discarded wire fields");
            }

            for (int length = 0; length < expected.Length; length++)
            {
                var truncated = expected.Take(length).ToArray();
                Reject(() => read(new ByteReader(truncated)));
            }

            var wrongCount = (byte[])expected.Clone();
            wrongCount[0] ^= 0xff;
            Reject(() => read(new ByteReader(wrongCount)));
            var wrongOpcode = (byte[])expected.Clone();
            wrongOpcode[2] ^= 1;
            Reject(() => read(new ByteReader(wrongOpcode)));
        }

        static void Reject(Action action)
        {
            try
            {
                action();
            }
            catch (InvalidDataException)
            {
                return;
            }
            catch (ArgumentException)
            {
                return;
            }

            throw new Exception("Malformed packet was accepted");
        }
    }
}
