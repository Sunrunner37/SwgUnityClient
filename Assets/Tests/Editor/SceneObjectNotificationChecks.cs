using Swg.Client.Core.Characters;
using Swg.Client.Core.Characters;
using Swg.Client.Runtime.World.SceneObjects;
using Swg.Client.Runtime;
using System.Collections.Generic;
using NUnit.Framework;
using Swg.Client.Core;
using Swg.Client.Core.Networking.Game.Replication;
using Swg.Client.Core.World;
using Swg.Client.Core.World.SceneObjects;
using UnityEngine;
using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;

namespace Swg.Client.Tests.Editor
{
    public sealed class SceneObjectNotificationChecks
    {
        [Test]
        public void ReparentingAnAncestorUpdatesPosesWithoutReparentingItsDescendants()
        {
            var registry = new SceneObjectRegistry();
            registry.Add(new CellSceneObject { SwgId = 10, Position = Vector3.right * 100 });
            registry.Add(new SpatialSceneObject { SwgId = 1, Position = Vector3.right });
            registry.Add(new SpatialSceneObject { SwgId = 2, ParentSwgId = 1, Position = Vector3.right * 2 });
            registry.Add(new SpatialSceneObject { SwgId = 3, ParentSwgId = 2, Position = Vector3.right * 3 });
            var changes = new Dictionary<SwgId, SceneObjectChange>();
            registry.ObjectChanged += (sceneObject, flags) => changes[sceneObject.SwgId] = flags;
            registry.Reparent(1, 10);
            Assert.That(changes[(SwgId)1], Is.EqualTo(SceneObjectChange.Containment));
            Assert.That(changes[(SwgId)2], Is.EqualTo(SceneObjectChange.Transform));
            Assert.That(changes[(SwgId)3], Is.EqualTo(SceneObjectChange.Transform));
            Assert.That(registry.GetServerWorldPose(3).position.x, Is.EqualTo(106));
        }

        [Test]
        public void ParentReplacementStillNotifiesDirectChildrenOfTheirNewParentInstance()
        {
            var registry = new SceneObjectRegistry();
            registry.Add(new SpatialSceneObject { SwgId = 1 });
            var child = new CreatureSceneObject { SwgId = 2, ParentSwgId = 1 };
            registry.Add(child);
            SceneObjectChange changes = SceneObjectChange.None;
            registry.Subscribe(2, flags => changes |= flags);
            var parent = new CreatureSceneObject { SwgId = 1 };
            registry.Replace(parent);
            Assert.That(child.Parent, Is.SameAs(parent));
            Assert.That(changes.HasFlag(SceneObjectChange.Containment), Is.True);
        }

        [Test]
        public void CrossingAnEmptyRoomDoesNotRepublishMissionWaypoints()
        {
            var registry = new SceneObjectRegistry();
            using var player = new CurrentPlayer(registry);
            player.BeginScene(1);
            registry.Add(new CellSceneObject { SwgId = 10 });
            registry.Add(new CreatureSceneObject { SwgId = 1 });
            registry.Add(new TangibleSceneObject { SwgId = 2, ParentSwgId = 1, TemplatePath = "object/tangible/datapad/shared_character_datapad.iff" });
            registry.Add(new MissionSceneObject { SwgId = 3, ParentSwgId = 2,
                Waypoint = new Waypoint(4, Vector3.zero, 1, "Mission", WaypointColor.Orange, true) });
            int refreshes = 0;
            player.WaypointsChanged += () => refreshes++;
            registry.SetServerWorldPose(1, 10, new Pose(Vector3.right, Quaternion.identity));
            Assert.That(refreshes, Is.Zero);
            registry.Reparent(3, default);
            Assert.That(refreshes, Is.EqualTo(1), "A real mission removal must still update waypoints.");
        }

        [Test]
        public void MovementNotifiesOnlyTheObjectAndItsDescendantsWithoutAppearanceWork()
        {
            using var world = GameServicesForTests.CreateReplication(GameServicesForTests.CreateState(out var currentPlayer), currentPlayer);
            world.Commit(1, new ReplicationLifecycleUpdate(objectType: (ReplicatedObjectType)(0x4352454f)));
            world.Commit(2, new ContainmentUpdate(parentSwgId: 1));
            world.Commit(3, new SceneObjectUpdate());
            world.ApplyPending();
            var changes = new Dictionary<SwgId, SceneObjectChange>();
            world.SceneObjects.ObjectChanged += (sceneObject, flags) => changes[sceneObject.SwgId] = flags;
            world.Commit(1, new MovementUpdate(position: new Vector3(10, 0, 0), movementCounter: 42));
            world.ApplyPending();
            Assert.That(changes.Keys, Is.EquivalentTo(new SwgId[] { 1, 2 }));
            Assert.That(changes[(SwgId)1], Is.EqualTo(SceneObjectChange.Transform));
            Assert.That(changes[(SwgId)2], Is.EqualTo(SceneObjectChange.Transform));
            Assert.That(world.SceneObjects.GetServerWorldPose(2).position, Is.EqualTo(new Vector3(10, 0, 0)));
        }

        [Test]
        public void ReparentAndWearableChangesInvalidateTheAffectedParents()
        {
            var registry = new SceneObjectRegistry();
            using var services = ManagedObjectTestScope.Use(registry);
            registry.Add(new CreatureSceneObject { SwgId = 1 });
            registry.Add(new CreatureSceneObject { SwgId = 2 });
            registry.Add(new TangibleSceneObject { SwgId = 3, ParentSwgId = 1 });
            var changes = new Dictionary<SwgId, SceneObjectChange>();
            registry.ObjectChanged += (sceneObject, flags) => changes[sceneObject.SwgId] = flags;
            registry.Reparent(3, 2);
            Assert.That(changes[(SwgId)1], Is.EqualTo(SceneObjectChange.Appearance));
            Assert.That(changes[(SwgId)2], Is.EqualTo(SceneObjectChange.Appearance));
            Assert.That(changes[(SwgId)3], Is.EqualTo(SceneObjectChange.Containment));
            changes.Clear();
            Assert.That(registry.TryGet(3, out var foundObject1), Is.True);
            registry.NotifyChanged(foundObject1, SceneObjectChange.Appearance);
            Assert.That(changes.Keys, Is.EquivalentTo(new SwgId[] { 2, 3 }));
        }

        [Test]
        public void ParentArrivalAndRemovalNotifyExistingChildren()
        {
            var registry = new SceneObjectRegistry();
            using var services = ManagedObjectTestScope.Use(registry);
            registry.Add(new SpatialSceneObject { SwgId = 2, ParentSwgId = 1 });
            var changes = new Dictionary<SwgId, SceneObjectChange>();
            registry.ObjectChanged += (sceneObject, flags) => changes[sceneObject.SwgId] = flags;
            registry.Add(new SpatialSceneObject { SwgId = 1, Position = Vector3.right * 10 });
            Assert.That(changes[(SwgId)2] & SceneObjectChange.Containment, Is.Not.EqualTo(SceneObjectChange.None));
            Assert.That(registry.GetServerWorldPose(2).position.x, Is.EqualTo(10));
            changes.Clear();
            registry.Remove(1);
            Assert.That(changes[(SwgId)2], Is.EqualTo(SceneObjectChange.Containment));
            Assert.That(registry.GetServerWorldPose(2).position.x, Is.EqualTo(0));
        }

        [Test]
        public void MovementInterpolatesVisualsAndSleepsAtRestWithoutChangingServerState()
        {
            var registry = new SceneObjectRegistry();
            using var services = ManagedObjectTestScope.Use(registry);
            var sceneObject = new SpatialSceneObject
            {
                SwgId = 1,
                Position = Vector3.right * 10,
                Rotation = Quaternion.Euler(0, 90, 0)
            };
            GameServices.Resolve<CurrentPlayer>().BeginScene(99);
            GameServices.Resolve<WorldState>().BeginScene(Vector3.right * 2, "terrain/test.trn");
            registry.Add(sceneObject);
            var instance = new GameObject("interpolation test");
            try
            {
                var tag = instance.AddComponent<ManagedSceneObject>();
                tag.Bind(1);
                var movement = instance.AddComponent<SceneObjectMovement>();
                movement.Initialize(sceneObject);
                registry.NotifyChanged(sceneObject, SceneObjectChange.Transform);
                Assert.That(instance.transform.localPosition, Is.EqualTo(Vector3.zero));
                movement.Advance(0.05f);
                Assert.That(instance.transform.localPosition.x, Is.EqualTo(4).Within(.001));
                Assert.That(Quaternion.Angle(instance.transform.localRotation, Quaternion.Euler(0, 45, 0)), Is.LessThan(.001));
                Assert.That(sceneObject.Position.x, Is.EqualTo(10));
                movement.Advance(0.05f);
                Assert.That(instance.transform.localPosition.x, Is.EqualTo(8).Within(.001));
                Assert.That(movement.enabled, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void MovementOwnsPoseNotificationsWithoutAManagedTag()
        {
            var registry = new SceneObjectRegistry();
            using var services = ManagedObjectTestScope.Use(registry);
            GameServices.Resolve<CurrentPlayer>().BeginScene(99);
            GameServices.Resolve<WorldState>().BeginScene(Vector3.right * 2, "terrain/test.trn");
            var parent = new SpatialSceneObject
            {
                SwgId = 1,
                Position = Vector3.right * 100
            };
            var child = new SpatialSceneObject
            {
                SwgId = 2,
                ParentSwgId = 1,
                Position = Vector3.right * 10
            };
            registry.Add(parent);
            registry.Add(child);
            var instance = new GameObject("independent movement");
            try
            {
                var movement = instance.AddComponent<SceneObjectMovement>();
                movement.Initialize(child);
                Assert.That(instance.transform.localPosition.x, Is.EqualTo(108));
                Assert.That(movement.enabled, Is.False);
                parent.Position = Vector3.right * 101;
                registry.NotifyChanged(parent, SceneObjectChange.Transform);
                Assert.That(movement.enabled, Is.True);
                movement.Advance(.1f);
                Assert.That(instance.transform.localPosition.x, Is.EqualTo(109));
                var replacement = new CreatureSceneObject
                {
                    SwgId = 2,
                    Position = Vector3.right * 20
                };
                registry.Replace(replacement);
                Assert.That(instance.transform.localPosition.x, Is.EqualTo(119));
                registry.Reparent(2, 0);
                Assert.That(instance.transform.localPosition.x, Is.EqualTo(18));
                movement.Unbind();
                replacement.Position = Vector3.right * 30;
                registry.NotifyChanged(replacement, SceneObjectChange.Transform);
                Assert.That(instance.transform.localPosition.x, Is.EqualTo(18));
                Assert.That(movement.enabled, Is.False);
                Assert.That(instance.GetComponent<ManagedSceneObject>(), Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void TeleportsAndContainmentChangesSnapAndNewTargetsStartAtCurrentVisualPose()
        {
            var instance = new GameObject("movement test");
            try
            {
                var movement = instance.AddComponent<SceneObjectMovement>();
                movement.SetTarget(new Pose(Vector3.right * 10, Quaternion.identity), 1, false);
                movement.Advance(.05f);
                movement.SetTarget(new Pose(Vector3.right * 15, Quaternion.identity), 1, false);
                movement.Advance(.05f);
                Assert.That(instance.transform.localPosition.x, Is.EqualTo(10).Within(.001));
                movement.SetTarget(new Pose(Vector3.right * 100, Quaternion.identity), 1, false);
                Assert.That(instance.transform.localPosition.x, Is.EqualTo(100));
                Assert.That(movement.enabled, Is.False);
                movement.SetTarget(new Pose(Vector3.right * 102, Quaternion.identity), 1.5f, true);
                Assert.That(instance.transform.localPosition.x, Is.EqualTo(102));
                Assert.That(instance.transform.localScale.x, Is.EqualTo(1.5f));
                Assert.That(movement.enabled, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }
    }
}
