using Swg.Client.Core.Networking.Game.Replication;
using Swg.Client.Core;
using Swg.Client.Core.World.SceneObjects;
using Swg.Client.Core.World;
using Swg.Client.Core.Networking.Game.Packets.Session;
using Swg.Client.Core.Networking.Game.Packets.Scene;
using Swg.Client.Core.Networking.Game.Packets.Replication;
using Swg.Client.Core.Networking.Game.Packets.Movement;
using UnityEngine.TestTools;
using System.Text.RegularExpressions;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Text;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking;
using Swg.Client.Core.Networking.Game;
using Swg.Client.Core.Networking.Game.Packets;

namespace Swg.Client.Tests.Editor
{
    public static class GamePacketChecks
    {
        public static void Run()
        {
            Golden(new SelectCharacterPacket(42), "0200768d09b52a00000000000000", SelectCharacterPacket.ReadFrom);
            Golden(new CmdSceneReadyPacket(), "0100221cfd43", CmdSceneReadyPacket.ReadFrom);
            Golden(new CmdStartScenePacket(42, "terrain/t.trn", new Vector3(1, 2, 3), "p", 99), "0900aedfe63a002a000000000000000d007465727261696e2f742e74726e0000803f00000040000040400100706300000000000000", CmdStartScenePacket.ReadFrom);
            Golden(new SceneCreateObjectPacket(42, Quaternion.identity, new Vector3(1, 2, 3), 123), "0500eadd89fe2a000000000000000000000000000000000000000000803f0000803f00000040000040407b00000000", SceneCreateObjectPacket.ReadFrom);
            Golden(new SceneCreateObjectPacket(42, new Quaternion(1, 2, 3, 4), new Vector3(1, 2, 3), 123), "0500eadd89fe2a000000000000000000803f0000004000004040000080400000803f00000040000040407b00000000", SceneCreateObjectPacket.ReadFrom);
            Golden(new BaselinesPacket(42, 0x4352454f, 3, new byte[] { 18, 0 }), "05000c5fa7682a000000000000004f45524303020000001200", BaselinesPacket.ReadFrom);
            Golden(new SceneEndBaselinesPacket(42), "02003760432c2a00000000000000", SceneEndBaselinesPacket.ReadFrom);
            Golden(new SceneDestroyObjectPacket(42), "030004d5454d2a0000000000000000", SceneDestroyObjectPacket.ReadFrom);
            Golden(new UpdateContainmentPacket(42, 7, 4), "04009edecb562a00000000000000070000000000000004000000", UpdateContainmentPacket.ReadFrom);
            var badLength = Encode(new BaselinesPacket(42, 0x4352454f, 3, new byte[] { 18, 0 }));
            Buffer.BlockCopy(BitConverter.GetBytes(3u), 0, badLength, 19, sizeof(uint));
            Reject(() => BaselinesPacket.ReadFrom(new ByteReader(badLength)));
            var badFloat = Encode(new SceneCreateObjectPacket(42, Quaternion.identity, new Vector3(1, 2, 3), 123));
            Buffer.BlockCopy(BitConverter.GetBytes(float.NaN), 0, badFloat, 30, sizeof(float));
            Reject(() => SceneCreateObjectPacket.ReadFrom(new ByteReader(badFloat)));
            CheckState(badLength);
        }

        static void CheckState(byte[] malformed)
        {
            var world = GameServicesForTests.CreateReplication(new WorldState(new SceneObjectRegistry()));
            var packets = new GamePacketRouter(GameServicesForTests.Create(world));
            packets.Accept(Encode(new CmdStartScenePacket(42, "terrain/corellia.trn", new Vector3(1, 2, 3), "object/creature/player/shared_human_female.iff", 99)));
            world.ApplyPending();
            Check(world.CaptureSceneStatus().StartScene.HasValue && !world.CaptureSceneStatus().PlayerBaselinesComplete, "Lightweight initial scene status");
            var first = world.CaptureSnapshot();
            Check(first.PlayerSwgId == 42 && !first.PlayerBaselinesComplete && !first.SceneReadyAcknowledged, "Start scene/readiness");
            packets.Accept(Encode(new SceneCreateObjectPacket(42, Quaternion.identity, new Vector3(1, 2, 3), 123)));
            world.ApplyPending();
            packets.Accept(Encode(new SceneCreateObjectPacket(7, Quaternion.identity, new Vector3(10, 0, 20), 456)));
            world.ApplyPending();
            packets.Accept(Encode(new UpdateContainmentPacket(42, 7, 4)));
            world.ApplyPending();
            packets.Accept(Encode(new BaselinesPacket(42, 0x4352454f, 3, Creature3())));
            world.ApplyPending();
            packets.Accept(Encode(new BaselinesPacket(42, 0x4352454f, 6, Creature6())));
            world.ApplyPending();
            var snapshot = world.CaptureSnapshot();
            var player = (CreatureSceneObject)snapshot.Objects.Single(o => o.SwgId == 42);
            Check(player.Name == "Misty", "Creature identity baseline");
            Check(Math.Abs(player.Scale - 1.1f) < .001f && player.Customization.SequenceEqual(new byte[] { 1, 0, 255, 3 }), "CREO3 customization/scale");
            Check(player.Equipment.Length == 1 && player.Equipment[0].SwgId == 99 && player.Equipment[0].TemplateCrc == 1234 && player.Equipment[0].Arrangement == 4, "CREO6 equipment fields");
            Check(player.ParentSwgId == 7 && !snapshot.PlayerBaselinesComplete, "Containment/early readiness");
            player.Customization[0] = 0;
            player.Equipment[0].Customization[0] = 0;
            var isolated = (CreatureSceneObject)world.CaptureSnapshot().Objects.Single(o => o.SwgId == 42);
            Check(isolated.Customization[0] == 1 && isolated.Equipment[0].Customization[0] == 1, "Snapshot mutable-array isolation");
            packets.Accept(Encode(new SceneEndBaselinesPacket(42)));
            world.ApplyPending();
            Check(world.CaptureSnapshot().PlayerBaselinesComplete && !world.CaptureSnapshot().SceneReadyAcknowledged, "Baseline completion is distinct from scene acknowledgement");
            packets.Accept(Encode(new UpdateTransformWithParentPacket(42, new Vector3(.125f, .25f, -.375f), 9, 0, 25, 7)));
            world.ApplyPending();
            player = (CreatureSceneObject)world.CaptureSnapshot().Objects.Single(o => o.SwgId == 42);
            Check(player.Position.x == .125f && player.Position.y == .25f && player.Position.z == -.375f && player.ParentSwgId == 7, "Compressed parent transform coordinate order");
            Check(Math.Abs(player.Rotation.y - .7071067f) < .001f && Math.Abs(player.Rotation.w - .7071067f) < .001f, "100-unit heading encoding");
            packets.Accept(Encode(new UpdateTransformPacket(42, new Vector3(5, 6, 7), 10, 0, 0)));
            world.ApplyPending();
            player = (CreatureSceneObject)world.CaptureSnapshot().Objects.Single(o => o.SwgId == 42);
            Check(player.ParentSwgId == 0 && player.Position.y == 6, "World transform resets containment");
            packets.Accept(Encode(new ObjectControllerPacket(0x1b, 0x71, 42, TransformPayload(false))));
            world.ApplyPending();
            player = (CreatureSceneObject)world.CaptureSnapshot().Objects.Single(o => o.SwgId == 42);
            Check(player.Position.x == 9 && player.Position.y == 10 && player.Position.z == 11, "Full object-controller transform");
            packets.Accept(Encode(new ObjectControllerPacket(0x1b, 0xf1, 42, TransformPayload(true))));
            world.ApplyPending();
            Check(world.CaptureSnapshot().Objects.Single(o => o.SwgId == 42).ParentSwgId == 7, "Full parent object-controller transform");
            long revision = world.Revision;
            packets.Accept(new byte[] { 0, 0, 0x11, 0x22, 0x33, 0x44 });
            world.ApplyPending();
            Check(world.Revision == revision, "Unknown opcode changed world revision");
            LogAssert.Expect(LogType.Error, new Regex("^Error when processing game packet:"));
            packets.Accept(malformed);
            world.ApplyPending();
            packets.Accept(Encode(new CmdSceneReadyPacket()));
            world.ApplyPending();
            Check(world.CaptureSceneStatus().SceneReadyAcknowledged && world.CaptureSceneStatus().PlayerBaselinesComplete, "Lightweight acknowledged scene status");
            Check(world.CaptureSnapshot().SceneReadyAcknowledged, "Server scene-ready acknowledgement");
            packets.Accept(Encode(new SceneDestroyObjectPacket(7)));
            world.ApplyPending();
            Check(world.CaptureSnapshot().Objects.All(o => o.SwgId != 7), "Destroy object");
            packets.Accept(Encode(new CmdStartScenePacket(43, "terrain/tatooine.trn", new Vector3(0, 0, 0), "p", 100)));
            world.ApplyPending();
            snapshot = world.CaptureSnapshot();
            Check(!world.CaptureSceneStatus().SceneReadyAcknowledged && !world.CaptureSceneStatus().PlayerBaselinesComplete, "Lightweight scene status reset");
            Check(snapshot.PlayerSwgId == 43 && snapshot.Objects.Count == 1 && !snapshot.SceneReadyAcknowledged && !snapshot.PlayerBaselinesComplete, "Zone reset retained old entities/readiness");
        }

        internal static byte[] Creature3()
        {
            using var memory = new MemoryStream();
            using var writer = new BinaryWriter(memory);
            writer.Write((ushort)18);
            writer.Write(1f);
            Ascii(writer, "");
            writer.Write(0u);
            Ascii(writer, "");
            writer.Write(5u);
            writer.Write(Encoding.Unicode.GetBytes("Misty"));
            writer.Write(1u);
            Packed(writer);
            writer.Write(0u);
            writer.Write(0u);
            writer.Write(0u);
            writer.Write(1u);
            writer.Write(0u);
            writer.Write(100u);
            writer.Write((byte)1);
            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write(0ul);
            writer.Write(1.1f);
            writer.Write(0u);
            writer.Write(0ul);
            IntVector(writer);
            return memory.ToArray();
        }

        internal static byte[] Creature6()
        {
            using var memory = new MemoryStream();
            using var writer = new BinaryWriter(memory);
            writer.Write((ushort)22);
            writer.Write(0x76u);
            writer.Write(0u);
            writer.Write(0u);
            writer.Write((ushort)1);
            Ascii(writer, "");
            Ascii(writer, "neutral");
            writer.Write(99ul);
            writer.Write(0ul);
            writer.Write(0ul);
            writer.Write(0ul);
            writer.Write(0u);
            writer.Write(0ul);
            writer.Write((byte)0);
            writer.Write(0u);
            writer.Write(0u);
            IntVector(writer);
            IntVector(writer);
            writer.Write(1u);
            writer.Write(0u);
            Packed(writer);
            writer.Write(4u);
            writer.Write(99ul);
            writer.Write(1234u);
            Ascii(writer, "");
            writer.Write((byte)0);
            return memory.ToArray();
        }

        static byte[] TransformPayload(bool parent)
        {
            using var memory = new MemoryStream();
            using var writer = new BinaryWriter(memory);
            writer.Write(11u);
            if (parent)
                writer.Write(7ul);
            writer.Write(0f);
            writer.Write(0f);
            writer.Write(0f);
            writer.Write(1f);
            writer.Write(9f);
            writer.Write(10f);
            writer.Write(11f);
            writer.Write(0f);
            return memory.ToArray();
        }

        static void IntVector(BinaryWriter writer)
        {
            writer.Write(9u);
            writer.Write(0u);
            for (int i = 0; i < 9; i++)
                writer.Write(1000u);
        }

        static void Ascii(BinaryWriter writer, string value)
        {
            writer.Write((ushort)value.Length);
            writer.Write(Encoding.ASCII.GetBytes(value));
        }

        static void Packed(BinaryWriter writer)
        {
            writer.Write((ushort)4);
            writer.Write(new byte[] { 1, 0, 255, 3 });
        }

        static byte[] Encode<T>(in T packet)
            where T : struct, IPacket
        {
            using var writer = new PacketWriter();
            return writer.Encode(in packet);
        }

        static void Golden<T>(T packet, string hex, Func<ByteReader, T> read)
            where T : struct, IPacket
        {
            var expected = TestHex.Decode(hex);
            Check(Encode(packet).SequenceEqual(expected), typeof(T).Name + " independent wire fixture");
            Check(Encode(read(new ByteReader(expected))).SequenceEqual(expected), typeof(T).Name + " round trip");
            for (int length = 0; length < expected.Length; length++)
            {
                var truncated = expected.Take(length).ToArray();
                Reject(() => read(new ByteReader(truncated)));
            }
        }

        static void Check(bool value, string message)
        {
            if (!value)
                throw new Exception(message);
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

            throw new Exception("Malformed game packet accepted");
        }
    }
}
