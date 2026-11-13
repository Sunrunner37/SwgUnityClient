using Swg.Client.Runtime.World.SceneObjects;
using Swg.Client.Core;
using Swg.Client.Runtime.World;
using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Swg.Client.Core.Networking.Game;
using Swg.Client.Core.Networking.Game.Packets.Scene;
using Swg.Client.Core.Networking.Game.Replication;
using Swg.Client.Core.World;
using Swg.Client.Core.World.SceneObjects;
using UnityEngine;
using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;

namespace Swg.Client.Tests.Editor
{
    public sealed class SceneObjectChecks
    {
        [Test]
        public void RegistryResolvesParentRelativePosesWithoutChangingLocalCoordinates()
        {
            var registry = new SceneObjectRegistry();
            var building = new TangibleSceneObject
            {
                SwgId = 1,
                Position = new Vector3(100, 10, 200),
                Rotation = Quaternion.Euler(0, 90, 0)
            };
            var cell = new SpatialSceneObject
            {
                SwgId = 2,
                ParentSwgId = 1,
                Position = new Vector3(0, 2, 4)
            };
            var player = new CreatureSceneObject
            {
                SwgId = 3,
                ParentSwgId = 2,
                Position = new Vector3(0, 3, 2)
            };
            registry.Add(building);
            registry.Add(cell);
            registry.Add(player);
            Pose pose = registry.GetServerWorldPose(3);
            Assert.That(Vector3.Distance(pose.position, new Vector3(106, 15, 200)), Is.LessThan(.001f));
            Assert.That(Quaternion.Angle(pose.rotation, building.Rotation), Is.LessThan(.001f));
            Assert.That(player.Position, Is.EqualTo(new Vector3(0, 3, 2)));
            Assert.That(player.ParentSwgId, Is.EqualTo(new SwgId(2)));
        }

        [TestCase(0x4352454fu, typeof(CreatureSceneObject))]
        [TestCase(0x54414e4fu, typeof(TangibleSceneObject))]
        [TestCase(0x4255494fu, typeof(TangibleSceneObject))]
        [TestCase(0x43454c4cu, typeof(SpatialSceneObject))]
        public void NetworkingConstructsConcreteApplicationTypes(uint wireType, Type expected)
        {
            using var world = GameServicesForTests.CreateReplication(new WorldState(new SceneObjectRegistry()));
            world.Commit(1, new ReplicationLifecycleUpdate(objectType: (ReplicatedObjectType)(wireType)));
            world.ApplyPending();
            Assert.That(world.SceneObjects.TryGet<SpatialSceneObject>(1, out var sceneObject), Is.True);
            Assert.That(sceneObject.GetType(), Is.EqualTo(expected));
            Assert.That(sceneObject.Scale, Is.EqualTo(1));
        }

        [Test]
        public void AssetSnapshotsAreDetachedAndDoNotCarryWorldGameObjects()
        {
            using var world = GameServicesForTests.CreateReplication(new WorldState(new SceneObjectRegistry()));
            world.StartScene(new CmdStartScenePacket(1, "terrain/corellia.trn", Vector3.zero, "player.iff", 0));
            world.Commit(1, new ReplicationLifecycleUpdate(objectType: (ReplicatedObjectType)(0x4352454f), baselinesComplete: true), objectUpdate: new CreatureSceneObjectUpdate { Customization = new byte[] { 1, 0, 255, 3 }, Equipment = new[] { new WorldEquipment(new byte[] { 1, 0, 255, 3 }, 4, 2, 5) } });
            world.ApplyPending();
            Assert.That(world.SceneObjects.TryGet<CreatureSceneObject>(1, out var live), Is.True);
            world.SceneObjects.Add(new SceneObject { SwgId = 2, ParentSwgId = live.SwgId });
            var first = (CreatureSceneObject)world.CaptureSnapshot().Objects[0];
            Assert.That(first.Parent, Is.Null);
            Assert.That(first.Children, Is.Empty);
            Assert.That(live.Children.Count, Is.EqualTo(1));
            first.Position = Vector3.one;
            first.Customization[0] = 0;
            first.Equipment[0].Customization[0] = 0;
            Assert.That(live.Position, Is.EqualTo(Vector3.zero));
            Assert.That(live.Customization[0], Is.EqualTo(1));
            Assert.That(live.Equipment[0].Customization[0], Is.EqualTo(1));
            Assert.That(world.CaptureSceneStatus().PlayerBaselinesComplete, Is.True);
        }

        [Test]
        public void BackgroundUpdatesWaitForTheMainThreadAndKeepTypedInstancesStable()
        {
            using var world = GameServicesForTests.CreateReplication(new WorldState(new SceneObjectRegistry()));
            var bytes = new byte[]
            {
                1,
                0,
                255,
                3
            };
            Task.Run(() => world.Commit(1, new ReplicationLifecycleUpdate(objectType: (ReplicatedObjectType)(0x4352454f)), objectUpdate: new TangibleSceneObjectUpdate { Customization = bytes })).GetAwaiter().GetResult();
            bytes[0] = 99;
            Assert.That(world.SceneObjects.TryGet(1, out _), Is.False);
            world.ApplyPending();
            Assert.That(world.SceneObjects.TryGet<CreatureSceneObject>(1, out var live), Is.True);
            Assert.That(live.Customization[0], Is.EqualTo(1), "Queued changes must own their arrays");
            Task.Run(() => world.Commit(1, new MovementUpdate(position: new Vector3(3, 4, 5)))).GetAwaiter().GetResult();
            Assert.That(live.Position, Is.EqualTo(Vector3.zero));
            world.ApplyPending();
            Assert.That(world.SceneObjects.TryGet(1, out var foundObject1), Is.True);
            Assert.That(foundObject1, Is.SameAs(live));
            Assert.That(live.Position, Is.EqualTo(new Vector3(3, 4, 5)));
        }

        [Test]
        public void InventoryAndDatapadEntitiesExistWithoutWorldObjects()
        {
            var registry = new SceneObjectRegistry();
            var inventory = new TangibleSceneObject
            {
                SwgId = 1
            };
            var weapon = new TangibleSceneObject
            {
                SwgId = 2,
                ParentSwgId = 1
            };
            SceneObject mission = new SceneObject
            {
                SwgId = 3
            };
            registry.Add(inventory);
            registry.Add(weapon);
            registry.Add(mission);
            Assert.That(registry.ChildrenOf(1), Does.Contain(weapon));
            Assert.That(mission is SpatialSceneObject, Is.False);
            Assert.That(ManagedSceneObject.TryGetObjectFrom(mission.SwgId, out _), Is.False);
            Assert.That(ManagedSceneObject.TryGetObjectFrom(weapon.SwgId, out _), Is.False);
            registry.Reparent(weapon.SwgId, SwgId.None);
            Assert.That(registry.TryGet(2, out var foundObject2), Is.True);
            Assert.That(foundObject2, Is.SameAs(weapon));
        }

        [Test]
        public void RegistryContainsAllEntitiesWhileNetworkingSnapshotsOnlyItsTrackedObjects()
        {
            using var world = GameServicesForTests.CreateReplication(new WorldState(new SceneObjectRegistry()));
            var building = new TangibleSceneObject
            {
                SwgId = 1
            };
            world.SceneObjects.Add(building);
            Assert.That(world.SceneObjects.Count, Is.EqualTo(1));
            Assert.That(world.SceneObjects, Is.EquivalentTo(new[] { building }));
            Assert.That(world.CaptureSnapshot().Objects, Is.Empty);
            world.Commit(1, new MovementUpdate(position: new Vector3(1, 2, 3)));
            world.ApplyPending();
            Assert.That(world.SceneObjects.TryGet(1, out var foundObject3), Is.True);
            Assert.That(foundObject3, Is.SameAs(building));
            Assert.That(world.SceneObjects.Count, Is.EqualTo(1));
            Assert.That(world.CaptureSnapshot().Objects.Count, Is.EqualTo(1));
            Assert.That(world.CaptureSnapshot().Objects[0].SwgId, Is.EqualTo(building.SwgId));
        }

        [Test]
        public void RemovalDetachesParentAndPreservesUnresolvedChildIdentity()
        {
            var registry = new SceneObjectRegistry();
            var parent = new SceneObject
            {
                SwgId = 1
            };
            var child = new SceneObject
            {
                SwgId = 2,
                ParentSwgId = 1
            };
            registry.Add(parent);
            registry.Add(child);
            registry.Remove(1);
            Assert.That(registry.TryGet(1, out _), Is.False);
            Assert.That(child.Parent, Is.Null);
            Assert.That(child.ParentSwgId, Is.EqualTo(new SwgId(1)));
            Assert.That(parent.Children, Is.Empty);
            var replacement = new SceneObject
            {
                SwgId = 1
            };
            registry.Add(replacement);
            Assert.That(child.Parent, Is.SameAs(replacement));
            Assert.That(replacement.Children, Is.EquivalentTo(new[] { child }));
        }
    }
}
