using Swg.Client.Core.Characters;
using Swg.Client.Core.Assets.Appearances;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Assets.Formats.Iff;
using Swg.Client.Core.Characters.Models;
using Swg.Client.Core.Geometry;
using Swg.Client.Core.Networking.Game;
using Swg.Client.Core.Networking.Game.Packets.Replication;
using Swg.Client.Core.Networking.Game.Replication;
using Swg.Client.Core.World;

namespace Swg.Client.Tests.Editor
{
    public sealed class EquipmentChecks
    {
        [Test]
        public void EquipmentDeltasApplyInOrderOnMainThreadAndNotifyAppearance()
        {
            using var world = GameServicesForTests.CreateReplication(GameServicesForTests.CreateState(out var currentPlayer), currentPlayer);
            using var services = GameServicesForTests.Create(world, currentPlayer);
            var router = new GamePacketRouter(services);
            world.Commit(1, new CreatureSceneObjectUpdate { Equipment = new[] { new WorldEquipment(new byte[] { 1 }, 4, 10, 100) } });
            world.ApplyPending();
            world.SceneObjects.TryGet<CreatureSceneObject>(1, out var creature);
            SceneObjectChange changes = SceneObjectChange.None;
            world.SceneObjects.Subscribe(1, value => changes |= value);
            byte[] Packet(Action<BinaryWriter> write)
            {
                using var stream = new MemoryStream();
                using var writer = new BinaryWriter(stream);
                write(writer);
                return stream.ToArray();
            }

            void Item(BinaryWriter writer, ulong swgId)
            {
                writer.Write((ushort)1);
                writer.Write((byte)2);
                writer.Write(4u);
                writer.Write(swgId);
                writer.Write(200u);
            }

            void Send(byte[] payload)
            {
                router.Accept(Packet(writer => new DeltasPacket(1, 0x4352454f, 6, payload).WriteTo(writer)));
            }

            Send(Packet(writer =>
            {
                writer.Write((ushort)1);
                writer.Write((ushort)15);
                writer.Write(3u);
                writer.Write(5u);
                writer.Write((byte)1);
                writer.Write((ushort)1);
                Item(writer, 20);
                writer.Write((byte)2);
                writer.Write((ushort)0);
                Item(writer, 30);
                writer.Write((byte)0);
                writer.Write((ushort)1);
            }));
            Assert.That(creature.Equipment.Single().SwgId.Value, Is.EqualTo(10));
            world.ApplyPending();
            Assert.That(creature.Equipment.Single().SwgId.Value, Is.EqualTo(30));
            Assert.That(changes.HasFlag(SceneObjectChange.Appearance), Is.True);
            Send(Packet(writer =>
            {
                writer.Write((ushort)1);
                writer.Write((ushort)15);
                writer.Write(1u);
                writer.Write(6u);
                writer.Write((byte)4);
            }));
            world.ApplyPending();
            Assert.That(creature.Equipment, Is.Empty);
        }

        [Test]
        public void HigherLayersHideCoveredTrianglesButDoNotOccludeThemselves()
        {
            IffNode Mesh(ushort layer, bool covers)
            {
                var version = new IffNode
                {
                    Kind = "0004"
                };
                var info = new byte[44];
                info[36] = 1;
                info[38] = 1;
                info[40] = covers ? (byte)1 : (byte)0;
                info[42] = (byte)layer;
                info[43] = (byte)(layer >> 8);
                version.Children.Add(new IffNode { Tag = "INFO", Data = info });
                version.Children.Add(new IffNode { Tag = "OZN ", Data = new byte[] { 102, 97, 99, 101, 0 } });
                version.Children.Add(new IffNode { Tag = "OZC ", Data = new byte[] { 1, 0, 0, 0 } });
                if (covers)
                    version.Children.Add(new IffNode { Tag = "ZTO ", Data = new byte[] { 0, 0 } });
                return version;
            }

            var result = new SkeletalAppearanceData();
            void Part(IffNode mesh, int[] combinations)
            {
                var part = new MeshPart { Triangles = new[] { 0, 1, 2, 2, 3, 0 } };
                result.Parts.Add(part);
                result.Sources.Add(new CachedMeshPartSource { Part = part, MeshVersion = mesh, TriangleOcclusionCombinations = combinations });
            }

            Part(Mesh(ushort.MaxValue, false), new[] { 0, -1 });
            Part(Mesh(2, true), new[] { 0, -1 });
            var attachment = new MeshPart { Triangles = new[] { 7, 8, 9 } };
            result.Parts.Insert(0, attachment);
            CharacterMeshOcclusion.Apply(result);
            Assert.That(result.Parts[0], Is.SameAs(attachment));
            Assert.That(attachment.Triangles, Is.EqualTo(new[] { 7, 8, 9 }));
            Assert.That(result.Parts[1].Triangles, Is.EqualTo(new[] { 2, 3, 0 }));
            Assert.That(result.Parts[2].Triangles.Length, Is.EqualTo(6));
        }
    }
}
