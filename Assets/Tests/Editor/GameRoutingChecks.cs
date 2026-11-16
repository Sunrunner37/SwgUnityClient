using Swg.Client.Core.Characters;
using Swg.Client.Core.Networking.Game.Handlers.Scene;
using Swg.Client.Core.Networking.Game.Handlers.Combat;
using Swg.Client.Core.Networking.Game.Packets.Combat;
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
using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;

namespace Swg.Client.Tests.Editor
{
    public sealed class GameRoutingChecks
    {
        [Test]
        public void CellPermissionsUpdateWithoutLosingTheNativeCellNumber()
        {
            var state = GameServicesForTests.CreateState(out var player);
            var cell = new CellSceneObject { SwgId = 100, Number = 7 };
            state.SceneObjects.Add(cell);
            var replication = GameServicesForTests.CreateReplication(state, player);
            try
            {
                IGamePacketHandler handler = new UpdateCellPermissionsPacketHandler(replication);
                byte[] Permission(bool allow)
                {
                    using var stream = new MemoryStream();
                    using var writer = new BinaryWriter(stream);
                    writer.Write((ushort)3); writer.Write(UpdateCellPermissionsPacket.Opcode);
                    writer.Write((byte)(allow ? 1 : 0)); writer.Write(100ul);
                    return stream.ToArray();
                }
                handler.Handle(new ByteReader(Permission(false)));
                Assert.That(cell.AllowEntry, Is.True, "Changes wait for the main thread.");
                replication.ApplyPending();
                Assert.That(cell.AllowEntry, Is.False);
                Assert.That(cell.Number, Is.EqualTo(7));
                handler.Handle(new ByteReader(Permission(true)));
                replication.ApplyPending();
                Assert.That(cell.AllowEntry, Is.True);
                var copy = new CellSceneObject();
                cell.CopyTo(copy);
                Assert.That(copy.AllowEntry, Is.True);
                Assert.That(copy.Number, Is.EqualTo(7));
            }
            finally { UnityEngine.Object.DestroyImmediate(replication.gameObject); }
        }

        [Test]
        public void WeaponRangesAreRetainedForTheLocalPlayerAndResetOnSceneEntry()
        {
            var state = GameServicesForTests.CreateState(out var player);
            player.BeginScene(42);
            var replication = GameServicesForTests.CreateReplication(state, player);
            try
            {
                using var stream = new MemoryStream();
                using var writer = new BinaryWriter(stream);
                writer.Write(123ul); writer.Write(20f); writer.Write(64f);
                writer.Write(-10); writer.Write(5); writer.Write(-30);
                IObjectControllerHandler handler = new WeaponRangesControllerHandler(replication);
                handler.Handle(new Swg.Client.Core.Networking.Game.Packets.ObjectControllerPacket(0x1b, WeaponRangesPacket.ControllerType, 42, stream.ToArray()));
                Assert.That(player.WeaponRanges.WeaponSwgId.IsNone, Is.True);
                replication.ApplyPending();
                var ranges = player.WeaponRanges;
                Assert.That(ranges.WeaponSwgId, Is.EqualTo((SwgId)123));
                Assert.That(ranges.IdealRange, Is.EqualTo(20));
                Assert.That(ranges.MaximumRange, Is.EqualTo(64));
                Assert.That(ranges.PointBlankAccuracy, Is.EqualTo(-10));
                Assert.That(ranges.IdealAccuracy, Is.EqualTo(5));
                Assert.That(ranges.MaximumRangeAccuracy, Is.EqualTo(-30));
                player.BeginScene(42);
                Assert.That(player.WeaponRanges.WeaponSwgId.IsNone, Is.True);
            }
            finally { UnityEngine.Object.DestroyImmediate(replication.gameObject); }
        }

        [TestCase(3)]
        [TestCase(6)]
        public void TruncatedBaselineNeverCommitsPartialValues(int view)
        {
            var world = GameServicesForTests.CreateReplication(GameServicesForTests.CreateState(out var currentPlayer), currentPlayer);
            var router = new GamePacketRouter(GameServicesForTests.Create(world, currentPlayer));
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
            var world = GameServicesForTests.CreateReplication(GameServicesForTests.CreateState(out var currentPlayer), currentPlayer);
            var router = new GamePacketRouter(GameServicesForTests.Create(world, currentPlayer));
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
            var world = GameServicesForTests.CreateReplication(GameServicesForTests.CreateState(out var currentPlayer), currentPlayer);
            var router = new GamePacketRouter(GameServicesForTests.Create(world, currentPlayer));
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
            Assert.That(((CreatureSceneObject)item).Posture, Is.EqualTo(CreaturePosture.Blocking));
            Assert.That(((SpatialSceneObject)item).Scale, Is.EqualTo(1.2f));
        }

        [Test]
        public void TangibleDeltaDoesNotReadCreatureFields()
        {
            var world = GameServicesForTests.CreateReplication(GameServicesForTests.CreateState(out var currentPlayer), currentPlayer);
            var router = new GamePacketRouter(GameServicesForTests.Create(world, currentPlayer));
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
            var world = GameServicesForTests.CreateReplication(GameServicesForTests.CreateState(out var currentPlayer), currentPlayer);
            var router = new GamePacketRouter(GameServicesForTests.Create(world, currentPlayer));
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
            var handler = new SceneCreateObjectPacketHandler(GameServicesForTests.CreateReplication(GameServicesForTests.CreateState(out var currentPlayer), currentPlayer));
            var error = Assert.Throws<InvalidOperationException>(() => PacketHandlerDiscovery.Map(new IGamePacketHandler[] { handler, handler }, item => item.Opcode));
            Assert.That(error.Message, Does.Contain(nameof(SceneCreateObjectPacketHandler)));
            Assert.That(error.Message, Does.Contain(SceneCreateObjectPacket.Opcode.ToString("X8")));
        }

        [Test]
        public void ReflectedHandlersHaveSeparateSessionDependencies()
        {
            var first = GameServicesForTests.CreateReplication(GameServicesForTests.CreateState(out var currentPlayer), currentPlayer);
            var second = GameServicesForTests.CreateReplication(GameServicesForTests.CreateState(out var secondPlayer), secondPlayer);
            var firstRouter = new GamePacketRouter(GameServicesForTests.Create(first, currentPlayer));
            var secondRouter = new GamePacketRouter(GameServicesForTests.Create(second, secondPlayer));
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
            var world = GameServicesForTests.CreateReplication(GameServicesForTests.CreateState(out var currentPlayer), currentPlayer);
            var types = PacketHandlerDiscovery.Create<IGamePacketHandler>(GameServicesForTests.Create(world, currentPlayer)).Select(handler => handler.GetType()).Concat(PacketHandlerDiscovery.Create<IObjectControllerHandler>(GameServicesForTests.Create(world, currentPlayer)).Select(handler => handler.GetType())).ToArray();
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
            var world = GameServicesForTests.CreateReplication(GameServicesForTests.CreateState(out var currentPlayer), currentPlayer);
            var router = new GamePacketRouter(GameServicesForTests.Create(world, currentPlayer));
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
