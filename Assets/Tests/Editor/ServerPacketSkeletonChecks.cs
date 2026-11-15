using Swg.Client.Core.Characters;
using Swg.Client.Core.World;
using Swg.Client.Core.Networking.Game.Replication;
using Swg.Client.Core;
using Swg.Client.Core.Networking.Game.Packets.Scene;
using UnityEngine.TestTools;
using System.Text.RegularExpressions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking;
using Swg.Client.Core.Networking.Game;
using Swg.Client.Core.Networking.Game.Handlers;
using Swg.Client.Core.Networking.Game.Packets;

namespace Swg.Client.Tests.Editor
{
    public sealed class ServerPacketSkeletonChecks
    {
        [Serializable]
        public sealed class Entry
        {
            public string kind;
            public uint value;
            public string name;
            public int[] operands;
            public string packetType;
            public string handlerType;
            public bool skeleton;
        }

        [Serializable]
        sealed class Inventory
        {
            public Entry[] packets;
        }

        static Entry[] Entries() => JsonUtility.FromJson<Inventory>(File.ReadAllText(Path.Combine(Application.dataPath, "Tests/Editor/Fixtures/Core3ServerPackets.json"))).packets;
        static IEnumerable<TestCaseData> Skeletons()
        {
            foreach (var entry in Entries().Where(item => item.skeleton))
                yield return new TestCaseData(entry).SetName("ServerSkeleton_" + entry.kind + "_" + entry.name);
        }

        [Test]
        public void EveryInventoriedServerActionHasOneDiscoveredHandler()
        {
            var world = GameServicesForTests.CreateReplication(GameServicesForTests.CreateState(out var currentPlayer), currentPlayer);
            var packets = PacketHandlerDiscovery.Map(PacketHandlerDiscovery.Create<IGamePacketHandler>(GameServicesForTests.Create(world, currentPlayer)), handler => handler.Opcode);
            var controllers = PacketHandlerDiscovery.Map(PacketHandlerDiscovery.Create<IObjectControllerHandler>(GameServicesForTests.Create(world, currentPlayer)), handler => handler.ControllerType);
            var entries = Entries();
            Assert.That(packets.Keys, Is.EquivalentTo(entries.Where(item => item.kind == "Packet").Select(item => item.value)));
            Assert.That(controllers.Keys, Is.EquivalentTo(entries.Where(item => item.kind == "Controller").Select(item => item.value)));
            foreach (var entry in entries)
            {
                object handler = entry.kind == "Packet" ? (object)packets[entry.value] : controllers[entry.value];
                Assert.That(handler.GetType().FullName, Is.EqualTo(entry.handlerType), entry.name);
            }
        }

        [TestCaseSource(nameof(Skeletons))]
        public void SkeletonIsTypedRoundTripsAndReportsWithoutMutatingWorld(Entry entry)
        {
            var world = GameServicesForTests.CreateReplication(GameServicesForTests.CreateState(out var currentPlayer), currentPlayer);
            var router = new GamePacketRouter(GameServicesForTests.Create(world, currentPlayer));
            var assembly = typeof(GamePacketRouter).Assembly;
            var type = assembly.GetType(entry.packetType, true);
            var read = type.GetMethod("ReadFrom", new[] { typeof(ByteReader) });
            Assert.That(read, Is.Not.Null, entry.name);
            foreach (int operands in entry.operands)
            {
                byte[] bytes = Fixture(entry, (ushort)operands);
                var packet = (IPacket)read.Invoke(null, new object[] { new ByteReader(bytes) });
                using var stream = new MemoryStream();
                using var writer = new BinaryWriter(stream);
                packet.WriteTo(writer);
                Assert.That(stream.ToArray(), Is.EqualTo(bytes), entry.name + " must preserve its parsed fields");
                if (entry.kind == "Packet")
                {
                    var handler = PacketHandlerDiscovery.Create<IGamePacketHandler>(GameServicesForTests.Create(world, currentPlayer)).Single(item => item.Opcode == entry.value);
                    Assert.Throws<NotImplementedException>(() => handler.Handle(new ByteReader(bytes)));
                }
                else
                {
                    var handler = PacketHandlerDiscovery.Create<IObjectControllerHandler>(GameServicesForTests.Create(world, currentPlayer)).Single(item => item.ControllerType == entry.value);
                    var envelope = ObjectControllerPacket.ReadFrom(new ByteReader(bytes));
                    Assert.Throws<NotImplementedException>(() => handler.Handle(envelope));
                }

                LogAssert.Expect(LogType.Warning, new Regex("^Unimplemented server action: " + Regex.Escape(entry.name)));
                router.Accept(bytes);
                // A recognized skeleton must reject a malformed header before attempting the action.
                bytes[0] = 255;
                bytes[1] = 255;
                LogAssert.Expect(LogType.Error, new Regex("^Error when processing game packet:"));
                router.Accept(bytes);
            }

            Assert.That(world.Revision, Is.Zero);
            Assert.That(world.CaptureSnapshot().Objects, Is.Empty);
        }

        [Test]
        public void UnimplementedActionDoesNotBlockFollowingImplementedPacket()
        {
            var entry = Entries().First(item => item.skeleton && item.kind == "Packet");
            var world = GameServicesForTests.CreateReplication(GameServicesForTests.CreateState(out var currentPlayer), currentPlayer);
            var router = new GamePacketRouter(GameServicesForTests.Create(world, currentPlayer));
            router.Accept(Fixture(entry, (ushort)entry.operands[0]));
            using var writer = new PacketWriter();
            var create = new SceneCreateObjectPacket(42, Quaternion.identity, Vector3.zero, 123);
            router.Accept(writer.Encode(in create));
            world.ApplyPending();
            Assert.That(world.CaptureSnapshot().Objects.Single().SwgId, Is.EqualTo(new SwgId(42)));
        }

        static byte[] Fixture(Entry entry, ushort operands)
        {
            var fixture = PacketFieldChecks.Fixtures().First(item => item.kind == entry.kind && item.value == entry.value);
            var bytes = TestHex.Decode(fixture.hex);
            bytes[0] = (byte)operands;
            bytes[1] = (byte)(operands >> 8);
            return bytes;
        }
    }
}
