using Swg.Client.Core.Networking.Game.Replication;
using Swg.Client.Core;
using Swg.Client.Core.World.SceneObjects;
using Swg.Client.Core.World;
using Swg.Client.Core.Networking.Game.Packets.Scene;
using Swg.Client.Core.Networking.Game.Packets.Replication;
using Swg.Client.Core.Networking.Game.Handlers.Scene;
using UnityEngine.TestTools;
using System.Text.RegularExpressions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking;
using Swg.Client.Core.Networking.Game;
using Swg.Client.Core.Networking.Game.Handlers;
using Swg.Client.Core.Networking.Game.Packets;

namespace Swg.Client.Tests.Editor
{
    public sealed class GameRoutingChecks
    {
        [TestCase(3)]
        [TestCase(6)]
        public void TruncatedBaselineNeverCommitsPartialValues(int view)
        {
            var world = GameServicesForTests.CreateReplication(new WorldState(new SceneObjectRegistry()));
            var router = new GamePacketRouter(GameServicesForTests.Create(world));
            router.Accept(Encode(new SceneCreateObjectPacket(42, Quaternion.identity, new Vector3(1, 2, 3), 123)));
            world.ApplyPending();
            long revision = world.Revision;
            byte[] payload = view == 3 ? GamePacketChecks.Creature3() : GamePacketChecks.Creature6();
            for (int length = 2; length < payload.Length; length++)
            {
                var truncated = payload.Take(length).ToArray();
                LogAssert.Expect(LogType.Error, new Regex("^Error when processing game packet:"));
                router.Accept(Encode(new BaselinesPacket(42, 0x4352454f, (byte)view, truncated)));
                world.ApplyPending();
                LogAssert.Expect(LogType.Error, new Regex("^Error when processing game packet:"));
                router.Accept(Encode(new BaselinesPacket(99, 0x4352454f, (byte)view, truncated)));
                world.ApplyPending();
                var snapshot = world.CaptureSnapshot();
                Assert.That(snapshot.Revision, Is.EqualTo(revision), "length " + length);
                Assert.That(snapshot.Objects.Count, Is.EqualTo(1));
                var item = snapshot.Objects.Single();
                Assert.That(item.Name, Is.Empty);
                Assert.That(item is TangibleSceneObject, Is.False);
                Assert.That(item is CreatureSceneObject, Is.False);
                Assert.That(item.GetType(), Is.EqualTo(typeof(SpatialSceneObject)));
            }

            router.Accept(Encode(new BaselinesPacket(42, 0x4352454f, (byte)view, payload)));
            world.ApplyPending();
            Assert.That(world.Revision, Is.GreaterThan(revision));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void InvalidDeltaRollsBackEarlierFields(bool unknownField)
        {
            var world = GameServicesForTests.CreateReplication(new WorldState(new SceneObjectRegistry()));
            var router = new GamePacketRouter(GameServicesForTests.Create(world));
            router.Accept(Encode(new BaselinesPacket(42, 0x4352454f, 3, GamePacketChecks.Creature3())));
            world.ApplyPending();
            long revision = world.Revision;
            byte[] payload = Delta(writer =>
            {
                writer.Write((ushort)2);
                writer.Write((ushort)2);
                WriteUnicode(writer, "Changed");
                writer.Write(unknownField ? (ushort)65535 : (ushort)14);
            // A height field without its float is malformed. An unknown field has no safe skip length.
            });
            LogAssert.Expect(LogType.Error, new Regex("^Error when processing game packet:"));
            router.Accept(Encode(new DeltasPacket(42, 0x4352454f, 3, payload)));
            world.ApplyPending();
            var snapshot = world.CaptureSnapshot();
            Assert.That(snapshot.Revision, Is.EqualTo(revision));
            Assert.That(snapshot.Objects.Single().Name, Is.EqualTo("Misty"));
        }

        [Test]
        public void ValidDeltaCommitsAllValuesWithOneRevision()
        {
            var world = GameServicesForTests.CreateReplication(new WorldState(new SceneObjectRegistry()));
            var router = new GamePacketRouter(GameServicesForTests.Create(world));
            router.Accept(Encode(new BaselinesPacket(42, 0x4352454f, 3, GamePacketChecks.Creature3())));
            world.ApplyPending();
            long revision = world.Revision;
            byte[] payload = Delta(writer =>
            {
                writer.Write((ushort)3);
                writer.Write((ushort)2);
                WriteUnicode(writer, "Changed");
                writer.Write((ushort)11);
                writer.Write((byte)4);
                writer.Write((ushort)14);
                writer.Write(1.2f);
            });
            router.Accept(Encode(new DeltasPacket(42, 0x4352454f, 3, payload)));
            world.ApplyPending();
            var snapshot = world.CaptureSnapshot();
            Assert.That(snapshot.Revision, Is.EqualTo(revision + 1));
            var item = snapshot.Objects.Single();
            Assert.That(item.Name, Is.EqualTo("Changed"));
            Assert.That(((CreatureSceneObject)item).Posture, Is.EqualTo(4));
            Assert.That(((SpatialSceneObject)item).Scale, Is.EqualTo(1.2f));
        }

        [Test]
        public void TangibleDeltaDoesNotReadCreatureFields()
        {
            var world = GameServicesForTests.CreateReplication(new WorldState(new SceneObjectRegistry()));
            var router = new GamePacketRouter(GameServicesForTests.Create(world));
            byte[] payload = Delta(writer =>
            {
                writer.Write((ushort)1);
                writer.Write((ushort)14);
                writer.Write(1.2f);
            });
            LogAssert.Expect(LogType.Error, new Regex("^Error when processing game packet:"));
            router.Accept(Encode(new DeltasPacket(42, 0x54414e4f, 3, payload)));
            world.ApplyPending();
            Assert.That(world.Revision, Is.Zero);
            Assert.That(world.CaptureSnapshot().Objects, Is.Empty);
        }

        [Test]
        public void InvalidSceneStartPreservesPreviousWorld()
        {
            var world = GameServicesForTests.CreateReplication(new WorldState(new SceneObjectRegistry()));
            var router = new GamePacketRouter(GameServicesForTests.Create(world));
            router.Accept(Encode(new CmdStartScenePacket(42, "terrain/t.trn", Vector3.zero, "player", 1)));
            world.ApplyPending();
            long revision = world.Revision;
            LogAssert.Expect(LogType.Error, new Regex("^Error when processing game packet:"));
            router.Accept(Encode(new CmdStartScenePacket(0, "terrain/t.trn", Vector3.zero, "player", 1)));
            world.ApplyPending();
            Assert.That(world.Revision, Is.EqualTo(revision));
            Assert.That(world.CaptureSnapshot().PlayerSwgId, Is.EqualTo(new SwgId(42)));
        }

        [Test]
        public void DuplicateOpcodeRegistrationFailsImmediately()
        {
            var handler = new SceneCreateObjectPacketHandler(GameServicesForTests.CreateReplication(new WorldState(new SceneObjectRegistry())));
            var error = Assert.Throws<InvalidOperationException>(() => PacketHandlerDiscovery.Map(new IGamePacketHandler[] { handler, handler }, item => item.Opcode));
            Assert.That(error.Message, Does.Contain(nameof(SceneCreateObjectPacketHandler)));
            Assert.That(error.Message, Does.Contain(SceneCreateObjectPacket.Opcode.ToString("X8")));
        }

        [Test]
        public void ReflectedHandlersHaveSeparateSessionDependencies()
        {
            var first = GameServicesForTests.CreateReplication(new WorldState(new SceneObjectRegistry()));
            var second = GameServicesForTests.CreateReplication(new WorldState(new SceneObjectRegistry()));
            var firstRouter = new GamePacketRouter(GameServicesForTests.Create(first));
            var secondRouter = new GamePacketRouter(GameServicesForTests.Create(second));
            firstRouter.Accept(Encode(new SceneCreateObjectPacket(42, Quaternion.identity, Vector3.zero, 123)));
            first.ApplyPending();
            Assert.That(first.CaptureSnapshot().Objects.Single().SwgId, Is.EqualTo(new SwgId(42)));
            Assert.That(second.CaptureSnapshot().Objects, Is.Empty);
            secondRouter.Accept(Encode(new SceneCreateObjectPacket(99, Quaternion.identity, Vector3.zero, 456)));
            second.ApplyPending();
            Assert.That(second.CaptureSnapshot().Objects.Single().SwgId, Is.EqualTo(new SwgId(99)));
            Assert.That(first.CaptureSnapshot().Objects.Single().SwgId, Is.EqualTo(new SwgId(42)));
        }

        [Test]
        public void ReflectedHandlersAndConstructorsArePreservedInPlayerBuilds()
        {
            var world = GameServicesForTests.CreateReplication(new WorldState(new SceneObjectRegistry()));
            var types = PacketHandlerDiscovery.Create<IGamePacketHandler>(GameServicesForTests.Create(world)).Select(handler => handler.GetType()).Concat(PacketHandlerDiscovery.Create<IObjectControllerHandler>(GameServicesForTests.Create(world)).Select(handler => handler.GetType())).ToArray();
            Assert.That(types, Is.Not.Empty);
            foreach (var type in types)
            {
                Assert.That(type.IsDefined(typeof(UnityEngine.Scripting.PreserveAttribute), false), Is.True, type.FullName);
                var constructor = type.GetConstructors().Single();
                Assert.That(constructor.IsDefined(typeof(UnityEngine.Scripting.PreserveAttribute), false), Is.True, type.FullName);
            }
        }

        [Test]
        public void UnsupportedMessagesDoNotChangeObjects()
        {
            var world = GameServicesForTests.CreateReplication(new WorldState(new SceneObjectRegistry()));
            var router = new GamePacketRouter(GameServicesForTests.Create(world));
            for (uint opcode = 1; opcode <= 40; opcode++)
                router.Accept(Delta(writer =>
                {
                    writer.Write((ushort)1);
                    writer.Write(opcode);
                }));
            router.Accept(Encode(new ObjectControllerPacket(0, 0xffffffff, 42, Array.Empty<byte>())));
            world.ApplyPending();
            LogAssert.Expect(LogType.Error, new Regex("^Error when processing game packet:"));
            router.Accept(new byte[] { 1 });
            world.ApplyPending();
            Assert.That(world.Revision, Is.Zero);
            Assert.That(world.CaptureSnapshot().Objects, Is.Empty);
        }

        static byte[] Delta(Action<BinaryWriter> write)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            write(writer);
            return stream.ToArray();
        }

        static void WriteUnicode(BinaryWriter writer, string value)
        {
            writer.Write((uint)value.Length);
            writer.Write(Encoding.Unicode.GetBytes(value));
        }

        static byte[] Encode<T>(in T packet)
            where T : struct, IPacket
        {
            using var writer = new PacketWriter();
            return writer.Encode(in packet);
        }
    }
}
