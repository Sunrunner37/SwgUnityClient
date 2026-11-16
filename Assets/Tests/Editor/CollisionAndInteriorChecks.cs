using System;
using Autofac;
using Swg.Client.Core.Assets.Tre;
using Swg.Client.Core.Configuration;
using Swg.Client.Runtime.World;
using Swg.Client.Runtime.World.Loading;
using Swg.Client.Runtime.Assets.Tre;
using Swg.Client.Runtime.Assets.Appearances;
using Swg.Client.Runtime.Assets.Shaders;
using Swg.Client.Core.Assets.Appearances;
using Swg.Client.Core.Networking.Game.Replication;
using Swg.Client.Core.Characters;
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
        [Test, Category("NativeAssets")]
        public void EarlyCellPermissionsDoNotDiscardSnapshotContainmentOrRoomContents()
        {
            var settings = ClientSettings.From(Path.Combine(Directory.GetParent(Application.dataPath).FullName, "client.ini"));
            using var loader = new TreAssetLoader(settings.TreDirectory, settings.TreArchivePriorities);
            var state = GameServicesForTests.CreateState(out var player);
            using var replication = GameServicesForTests.CreateReplication(state, player);
            replication.Commit(1855675, new CellSceneObjectUpdate { AllowEntry = false });
            replication.ApplyPending();
            Assert.That(state.SceneObjects.TryGet<CellSceneObject>(1855675, out var earlyCell), Is.True);
            state.Snapshot = WorldSnapshot.Load(loader, "corellia");
            WorldContentLoader.PopulateSnapshot(state);
            Assert.That(state.SceneObjects.TryGet<CellSceneObject>(1855675, out var cell), Is.True);
            Assert.That(cell, Is.SameAs(earlyCell));
            Assert.That(cell.AllowEntry, Is.False);
            Assert.That(cell.ParentSwgId, Is.EqualTo(new SwgId(1855671)));
            Assert.That(cell.TemplatePath, Is.EqualTo("object/cell/shared_cell.iff"));
            Assert.That(cell.Number, Is.EqualTo(4));
            var contents = state.Snapshot.Entries.Where(entry => entry.ParentSwgId == cell.SwgId).ToArray();
            Assert.That(contents, Is.Not.Empty);
            foreach (var entry in contents)
                Assert.That(Vector3.Distance(state.SceneObjects.GetServerWorldPose(entry.SwgId).position, entry.WorldPosition), Is.LessThan(.001f), entry.TemplatePath);
            WorldContentLoader.PopulateSnapshot(state);
            Assert.That(cell.AllowEntry, Is.False);
            Assert.That(cell.Parent.Children.Count(child => child.SwgId == cell.SwgId), Is.EqualTo(1));
        }

        [Test, Category("NativeAssets")]
        public void StarportExteriorShellIsInactiveInsideAndReturnsOnExit()
        {
            var settings = ClientSettings.From(Path.Combine(Directory.GetParent(Application.dataPath).FullName, "client.ini"));
            using var assets = new TreRuntimeAssets(settings);
            assets.Ready.GetAwaiter().GetResult();
            var appearance = AppearanceLoader.LoadDetailed(assets.Loader, "object/building/corellia/shared_starport_corellia.iff");
            Assert.That(appearance.Parts.All(part => part.PortalCellNumber.HasValue), Is.True);
            Assert.That(appearance.Parts.Count(part => part.PortalCellNumber == 0), Is.GreaterThan(0));
            var registry = new SceneObjectRegistry();
            var context = new WorldRenderingContext();
            context.Initialize(null, null, Shader.Find("Sprites/Default"), null, null, null);
            using var services = ManagedObjectTestScope.Use(registry, builder => builder.RegisterInstance(context));
            GameServices.Resolve<CurrentPlayer>().BeginScene(1);
            var building = new TangibleSceneObject { SwgId = 100 };
            registry.Add(building);
            registry.Add(new CellSceneObject { SwgId = 101, ParentSwgId = 100, Number = 1 });
            registry.Add(new CreatureSceneObject { SwgId = 1 });
            using var resources = new AppearanceResources(appearance, new SwgMaterialFactory(assets), Shader.Find("SWG/World"), Shader.Find("SWG/Character"), Shader.Find("SWG/Particle"));
            var root = resources.Create(null);
            try
            {
                WorldSceneObjectRenderer.ConfigureCollision(building, root);
                root.AddComponent<BuildingInterior>().Initialize(100, appearance.PortalLayout);
                var exterior = root.transform.Find(BuildingInterior.ExteriorCellName).gameObject;
                Assert.That(exterior.activeSelf, Is.True);
                Assert.That(exterior.GetComponentsInChildren<MeshFilter>().Length, Is.EqualTo(appearance.Parts.Count(part => part.PortalCellNumber == 0)));
                registry.Reparent(1, 101);
                Assert.That(exterior.activeInHierarchy, Is.False);
                Assert.That(root.GetComponentsInChildren<MeshFilter>().Length, Is.GreaterThan(0), "Room geometry remains visible.");
                Assert.That(root.GetComponentsInChildren<MeshCollider>().Any(collider => collider.TryGetComponent<BuildingFloor>(out _)), Is.True);
                Assert.That(root.GetComponentsInChildren<MeshCollider>().Any(collider => collider.transform.IsChildOf(exterior.transform)), Is.False);
                registry.Reparent(1, 0);
                Assert.That(exterior.activeSelf, Is.True);
                Assert.That(exterior.GetComponentsInChildren<MeshCollider>(), Is.Not.Empty);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void OrbitCameraIgnoresTheFollowedCapsuleButStillFindsScenery(bool nestedCapsule)
        {
            var actor = new GameObject("Camera followed character");
            var cameraRoot = new GameObject("Camera collision test");
            var wall = new GameObject("Camera blocking scenery");
            GameObject capsuleRoot = nestedCapsule ? new GameObject("Character capsule") : actor;
            try
            {
                actor.transform.position = new Vector3(10000, 10000, 10000);
                if (nestedCapsule) capsuleRoot.transform.SetParent(actor.transform, false);
                // Match the movement capsule shape using a collider available to Edit Mode queries.
                var capsule = capsuleRoot.AddComponent<CapsuleCollider>();
                capsule.height = 1.7f;
                capsule.radius = .25f;
                capsule.center = Vector3.up * .88f;
                var camera = cameraRoot.AddComponent<WorldOrbitCamera>();
                camera.target = actor.transform;
                // Reproduce the focus/capsule offset around a predicted movement update.
                var focus = actor.transform.position + Vector3.up * 1.4f + Vector3.forward * .5f;
                var direction = -(Quaternion.Euler(20, 0, 0) * Vector3.forward);
                var desired = focus + direction * 7;
                wall.transform.position = focus + direction * 3;
                wall.AddComponent<BoxCollider>();
                wall.SetActive(false);
                Physics.SyncTransforms();
                Assert.That(Physics.SphereCast(focus, .18f, direction, out var selfHit, 7, ~0, QueryTriggerInteraction.Ignore), Is.True);
                Assert.That(selfHit.collider, Is.SameAs(capsule));
                Assert.That(selfHit.distance, Is.LessThan(.1f), "The old camera jumped into the character on this hit.");
                Assert.That(Vector3.Distance(camera.ConstrainToObstacles(focus, desired), desired), Is.LessThan(.001f));
                wall.SetActive(true);
                Physics.SyncTransforms();
                float constrained = Vector3.Distance(focus, camera.ConstrainToObstacles(focus, desired));
                Assert.That(constrained, Is.InRange(1.5f, 3f), "Ignoring the capsule must not discard the wall behind it.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(actor);
                UnityEngine.Object.DestroyImmediate(cameraRoot);
                UnityEngine.Object.DestroyImmediate(wall);
            }
        }

        [Test]
        public void OrbitCameraIgnoresTheFollowedPlayersMount()
        {
            var actor = new GameObject("Mounted camera target");
            var cameraRoot = new GameObject("Mounted camera collision test");
            var mount = new GameObject("Camera followed vehicle");
            try
            {
                var camera = cameraRoot.AddComponent<WorldOrbitCamera>();
                camera.target = actor.transform;
                var focus = new Vector3(10000, 10000, 10000);
                var desired = focus + Vector3.back * 7;
                mount.transform.position = focus + Vector3.back * 2;
                mount.AddComponent<BoxCollider>();
                Physics.SyncTransforms();
                Assert.That(Vector3.Distance(camera.ConstrainToObstacles(focus, desired), desired), Is.GreaterThan(1));
                Assert.That(Vector3.Distance(camera.ConstrainToObstacles(focus, desired, mount.transform), desired), Is.LessThan(.001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(actor);
                UnityEngine.Object.DestroyImmediate(cameraRoot);
                UnityEngine.Object.DestroyImmediate(mount);
            }
        }

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

        [TestCase(0, false)]
        [TestCase(0, true)]
        [TestCase(1, false)]
        [TestCase(1, true)]
        [TestCase(2, false)]
        [TestCase(2, true)]
        [Category("NativeAssets")]
        public void GuildHallMountedBarriersBlockBothApproaches(int entrance, bool reverse)
        {
            var settings = ClientSettings.From(Path.Combine(Directory.GetParent(Application.dataPath).FullName, "client.ini"));
            using var loader = new TreAssetLoader(settings.TreDirectory, settings.TreArchivePriorities);
            var layout = PortalLayout.Parse(IffNode.Form(IffNode.Parse(loader.Read("appearance/ply_corl_assoc_hall_civ_s01.pob")), "PRTO"), loader.Read);
            var context = new WorldRenderingContext();
            context.Initialize(null, null, Shader.Find("Sprites/Default"), null, null, null);
            using var services = ManagedObjectTestScope.Use(new SceneObjectRegistry(), builder => builder.RegisterInstance(context));
            var root = new GameObject("Guild hall mounted entrance test");
            root.transform.position = new Vector3(10000, 0, 10000);
            try
            {
                var building = new GameObject("Guild hall");
                building.transform.SetParent(root.transform, false);
                building.AddComponent<BuildingInterior>().Initialize(100, layout);
                var barriers = building.GetComponentsInChildren<MeshCollider>(true).Where(c => c.name == "Mounted entry barrier").ToArray();
                Assert.That(barriers.Length, Is.EqualTo(3));
                foreach (var barrier in barriers) barrier.gameObject.SetActive(true);
                var polygon = layout.Entrances[entrance];
                var center = polygon.Aggregate(Vector3.zero, (sum, point) => sum + point) / polygon.Length;
                var normal = Vector3.Cross(polygon[1] - polygon[0], polygon[2] - polygon[0]).normalized * (reverse ? -1 : 1);
                var actor = new GameObject("Mounted capsule");
                actor.transform.SetParent(root.transform, false);
                actor.transform.localPosition = center + normal * 2 - Vector3.up * .8f;
                var collision = actor.AddComponent<PlayerCollision>();
                collision.Initialize(GameServices.Resolve<WorldState>(), true);
                // No gravity here: isolate the real portal polygon from stair/floor grounding.
                for (int i = 0; i < 40; i++) collision.Move(-normal, .1f, 0);
                float side = Vector3.Dot(actor.transform.position - root.transform.position - center, normal);
                Assert.That(side, Is.GreaterThan(.5f), "The mounted capsule must remain on its approach side of the native doorway.");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [TestCase(0)]
        [TestCase(1)]
        public void MountedMovementAllowsExteriorFloorsButRejectsInteriorFloors(int cellNumber)
        {
            var root = new GameObject("Mounted floor containment test");
            root.transform.position = new Vector3(10000, 0, 10000);
            try
            {
                Box(root.transform, "Exterior", new Vector3(0, -.05f, -2), new Vector3(10, .1f, 4));
                var room = Box(root.transform, "Native building floor", new Vector3(0, -.05f, 2), new Vector3(10, .1f, 4));
                room.AddComponent<BuildingFloor>().cellNumber = cellNumber;
                var actor = new GameObject("Mounted capsule");
                actor.transform.SetParent(root.transform, false);
                actor.transform.localPosition = Vector3.back;
                var collision = actor.AddComponent<PlayerCollision>();
                collision.Initialize(new WorldState(new SceneObjectRegistry()), true);
                for (int i = 0; i < 40; i++) collision.Move(Vector3.forward, .1f, 1f / 60);
                float z = actor.transform.localPosition.z;
                Assert.That(z, cellNumber == 0 ? Is.GreaterThan(2) : Is.LessThan(0), "Only interior floors should block mounted prediction when a portal barrier is missing.");
                Assert.That(collision.CellSwgId, Is.EqualTo(default(SwgId)));
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
