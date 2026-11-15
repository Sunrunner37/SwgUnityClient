using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Swg.Client.Core;
using Swg.Client.Core.Assets.Formats.Iff;
using Swg.Client.Core.Assets.Formats.Pob;
using Swg.Client.Core.World;
using Swg.Client.Core.World.SceneObjects;
using Swg.Client.Runtime;
using Swg.Client.Runtime.World.SceneObjects;
using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;

namespace Swg.Client.Tests.Editor
{
    public sealed class CollisionAndInteriorChecks
    {
        [TestCase(true)]
        [TestCase(false)]
        public void CellTransitionsPublishOneConsistentWorldPose(bool entering)
        {
            var registry = new SceneObjectRegistry();
            using var services = ManagedObjectTestScope.Use(registry);
            var state = GameServices.Resolve<WorldState>();
            state.BeginScene(new Vector3(5, 0, 3), "terrain/test.trn");
            registry.Add(new SpatialSceneObject { SwgId = 1, Position = new Vector3(100, 20, -300), Rotation = Quaternion.Euler(0, 90, 0) });
            registry.Add(new CellSceneObject { SwgId = 10, ParentSwgId = 1, Number = 1 });
            var actor = new SpatialSceneObject { SwgId = 2, ParentSwgId = entering ? new SwgId(0) : new SwgId(10) };
            registry.Add(actor);
            var instance = new GameObject("Atomic room transition");
            try
            {
                instance.AddComponent<SceneObjectMovement>().Initialize(actor);
                var observed = new List<Pose>();
                var flags = new List<SceneObjectChange>();
                registry.Subscribe(2, changes => { observed.Add(registry.GetServerWorldPose(2)); flags.Add(changes); });
                var expected = new Pose(new Vector3(120, 23, -320), Quaternion.Euler(0, 35, 0));
                long revision = registry.Revision;
                registry.SetServerWorldPose(2, entering ? new SwgId(10) : new SwgId(0), expected);
                Assert.That(observed.Count, Is.EqualTo(1));
                Assert.That(Vector3.Distance(observed[0].position, expected.position), Is.LessThan(.001));
                Assert.That(Quaternion.Angle(observed[0].rotation, expected.rotation), Is.LessThan(.001));
                Assert.That(flags.Single(), Is.EqualTo(SceneObjectChange.Transform | SceneObjectChange.Containment));
                Assert.That(registry.Revision, Is.EqualTo(revision + 1));
                Assert.That(Vector3.Distance(instance.transform.position, expected.position - state.RenderOrigin), Is.LessThan(.001));
                Assert.That(actor.ParentSwgId, Is.EqualTo(entering ? new SwgId(10) : new SwgId(0)));
            }
            finally { UnityEngine.Object.DestroyImmediate(instance); }
        }

        [Test]
        public void AtomicWorldPosePreservesCore3AbsoluteMountedCoordinates()
        {
            var registry = new SceneObjectRegistry();
            registry.Add(new CreatureSceneObject { SwgId = 1, Position = new Vector3(100, 0, 50), Rotation = Quaternion.Euler(0, 90, 0) });
            var rider = new CreatureSceneObject { SwgId = 2 };
            registry.Add(rider);
            var expected = new Pose(new Vector3(110, 2, 55), Quaternion.identity);
            registry.SetServerWorldPose(2, 1, expected);
            Assert.That(rider.Position, Is.EqualTo(expected.position));
            Assert.That(registry.GetServerWorldPose(2).position, Is.EqualTo(expected.position));
        }

        [Test]
        public void FurnitureDoesNotReplaceTheNativeRoomFloor()
        {
            var root = new GameObject("Interior floor query");
            root.transform.position = new Vector3(10000, 0, 10000);
            try
            {
                var ground = Box(root.transform, "Native floor", new Vector3(0, -.05f, 0), new Vector3(5, .1f, 5));
                var floor = ground.AddComponent<BuildingFloor>();
                floor.cellNumber = 7;
                Box(root.transform, "Chest", Vector3.up * .4f, new Vector3(1, .8f, 1));
                Physics.SyncTransforms();
                var hits = new RaycastHit[16];
                int count = Physics.RaycastNonAlloc(root.transform.position + Vector3.up * 1.4f, Vector3.down, hits, 8, ~0, QueryTriggerInteraction.Ignore);
                Assert.That(count, Is.GreaterThanOrEqualTo(2));
                Assert.That(PlayerCollision.FindBuildingFloor(hits, count), Is.SameAs(floor));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [Test]
        public void CollisionComponentStopsAtWallsAndReturnsTheResolvedPosition()
        {
            var root = new GameObject("Controlled collision test");
            root.transform.position = new Vector3(10000, 0, 10000);
            try
            {
                Box(root.transform, "Ground", Vector3.down * .5f, new Vector3(20, 1, 20));
                Box(root.transform, "Wall", new Vector3(0, 1.5f, 2), new Vector3(6, 3, .3f));
                var actor = new GameObject("Actor");
                actor.transform.SetParent(root.transform, false);
                var collision = actor.AddComponent<PlayerCollision>();
                collision.Initialize(new WorldState(new SceneObjectRegistry()), false);
                Vector3 resolved = actor.transform.position;
                for (int i = 0; i < 60; i++) resolved = collision.Move(Vector3.forward, .1f, 1f / 60);
                float distance = resolved.z - root.transform.position.z;
                Assert.That(distance, Is.GreaterThan(1));
                Assert.That(distance, Is.LessThan(1.8f));
                Assert.That(resolved, Is.EqualTo(actor.transform.position));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [Test]
        public void MissingFloorPreservesCellAppearanceAndFollowingCellNumbers()
        {
            IffNode Form(string kind, params IffNode[] children)
            {
                var node = new IffNode { Tag = "FORM", Kind = kind };
                node.Children.AddRange(children);
                return node;
            }
            IffNode Cell(string name, string appearance, string floor = null)
            {
                using var bytes = new MemoryStream();
                using var writer = new BinaryWriter(bytes);
                void CString(string value) { writer.Write(Encoding.ASCII.GetBytes(value)); writer.Write((byte)0); }
                writer.Write(0u);
                writer.Write((byte)0);
                CString(name);
                CString(appearance);
                writer.Write((byte)(floor == null ? 0 : 1));
                if (floor != null) CString(floor);
                return Form("CELL", Form("0005", new IffNode { Tag = "DATA", Data = bytes.ToArray() }));
            }
            var root = Form("PRTO", Form("0004", Form("PRTS"), Form("CELS",
                Cell("exterior", "appearance/exterior.msh", "appearance/missing.flr"),
                Cell("hall", "appearance/hall.msh"))));
            LogAssert.Expect(LogType.Warning, "Portal cell 0 floor appearance/missing.flr: test missing; keeping its visual appearance.");
            int reads = 0;
            var layout = PortalLayout.Parse(root, path => { reads++; throw new FileNotFoundException("test missing"); });
            Assert.That(reads, Is.EqualTo(1));
            Assert.That(layout.Cells[0].AppearancePath, Is.EqualTo("appearance/exterior.msh"));
            Assert.That(layout.Cells[0].Floor, Is.Null);
            Assert.That(layout.Cells[1].Number, Is.EqualTo(1));
            Assert.That(layout.Cells[1].AppearancePath, Is.EqualTo("appearance/hall.msh"));
        }

        static GameObject Box(Transform parent, string name, Vector3 position, Vector3 size)
        {
            var box = new GameObject(name);
            box.transform.SetParent(parent, false);
            box.transform.localPosition = position;
            box.AddComponent<BoxCollider>().size = size;
            return box;
        }
    }
}
