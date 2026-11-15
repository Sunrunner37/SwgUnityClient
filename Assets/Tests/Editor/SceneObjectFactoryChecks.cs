using Swg.Client.Core.Characters;
using System;
using NUnit.Framework;
using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;
using Swg.Client.Core;
using Swg.Client.Core.Networking.Game;
using Swg.Client.Core.Networking.Game.Replication;
using Swg.Client.Core.World;
using Swg.Client.Core.World.SceneObjects;
using UnityEngine;

namespace Swg.Client.Tests.Editor
{
    public sealed class SceneObjectFactoryChecks
    {
        [TestCase("object/building/corellia/shared_house.iff", typeof(TangibleSceneObject))]
        [TestCase("object/creature/player/shared_human_female.iff", typeof(CreatureSceneObject))]
        [TestCase("object/tangible/inventory/shared_character_inventory.iff", typeof(TangibleSceneObject))]
        [TestCase("object/tangible/datapad/shared_character_datapad.iff", typeof(TangibleSceneObject))]
        [TestCase("object/waypoint/shared_waypoint.iff", typeof(SceneObject))]
        [TestCase("object/mission/shared_mission.iff", typeof(MissionSceneObject))]
        [TestCase("object/intangible/vehicle/shared_speederbike.iff", typeof(IntangibleSceneObject))]
        public void TemplateFamiliesSelectConcreteTypes(string path, Type expected)
        {
            var result = SceneObjectFactory.Create(10, path);
            Assert.That(result.GetType(), Is.EqualTo(expected));
            Assert.That(result.SwgId, Is.EqualTo(new SwgId(10)));
            Assert.That(result.TemplatePath, Is.EqualTo(path));
        }

        [Test]
        public void QueuedPacketsApplyAllFieldsBeforePublishingTheRefinedCreature()
        {
            using var world = GameServicesForTests.CreateReplication(GameServicesForTests.CreateState(out var currentPlayer), currentPlayer);
            CreatureSceneObject published = null;
            world.SceneObjects.ObjectChanged += (sceneObject, changes) =>
            {
                if (published == null && sceneObject is CreatureSceneObject)
                {
                    published = (CreatureSceneObject)sceneObject.Copy();
                    Assert.That(sceneObject.ParentSwgId, Is.EqualTo(new SwgId(20)));
                }
            };
            world.Commit(10, new ContainmentUpdate(parentSwgId: 20, arrangement: 4));
            world.Commit(10, new ReplicationLifecycleUpdate(objectType: (ReplicatedObjectType)(0x4352454f), templateCrc: 123), objectUpdate: new CreatureSceneObjectUpdate { Name = "Creature", Height = 1.25f, Customization = new byte[] { 1, 2 }, Equipment = new[] { new WorldEquipment(new byte[] { 3 }, 4, 99, 5) }, AlternateAppearance = "appearance/test.iff", Posture = 7 }, initialMovement: new MovementUpdate(position: new Vector3(1, 2, 3), rotation: Quaternion.Euler(0, 90, 0)));
            world.ApplyPending();
            Assert.That(published, Is.Not.Null);
            Assert.That(published.TemplateCrc, Is.EqualTo(123));
            Assert.That(published.Position, Is.EqualTo(new Vector3(1, 2, 3)));
            Assert.That(Quaternion.Angle(published.Rotation, Quaternion.Euler(0, 90, 0)), Is.LessThan(.001));
            Assert.That(published.Name, Is.EqualTo("Creature"));
            Assert.That(published.Scale, Is.EqualTo(1.25f));
            Assert.That(published.Arrangement, Is.EqualTo(4));
            Assert.That(published.Customization, Is.EqualTo(new byte[] { 1, 2 }));
            Assert.That(published.Equipment[0].SwgId, Is.EqualTo(new SwgId(99)));
            Assert.That(published.AlternateAppearance, Is.EqualTo("appearance/test.iff"));
            Assert.That(published.Posture, Is.EqualTo(CreaturePosture.LyingDown));
        }

        [Test]
        public void StaticFactoryInitializesIdentityContainmentAndPose()
        {
            var entry = new SnapshotEntry
            {
                SwgId = 10,
                ParentSwgId = 20,
                TemplatePath = "object/building/corellia/shared_house.iff",
                Position = Vector3.one,
                Rotation = Quaternion.Euler(0, 30, 0)
            };
            var result = SceneObjectFactory.Create(entry);
            Assert.That(result.SwgId, Is.EqualTo(entry.SwgId));
            Assert.That(result.ParentSwgId, Is.EqualTo(entry.ParentSwgId));
            Assert.That(result.Name, Is.EqualTo(entry.TemplatePath));
            Assert.That(result.Position, Is.EqualTo(entry.Position));
            Assert.That(result.Rotation, Is.EqualTo(entry.Rotation));
        }

        [Test]
        public void UnknownMetadataKeepsExistingIntangibleAndExistingReferences()
        {
            var current = SceneObjectFactory.Create(10, "object/mission/shared_mission.iff");
            var updated = ReplicatedSceneObjectFactory.Refine(current, null, templateCrc: 123);
            Assert.That(updated, Is.SameAs(current));
        }

        [Test]
        public void LookupVariantsReturnOnlyCompatibleObjectsWithoutAllocationsOrSideEffects()
        {
            var registry = new SceneObjectRegistry();
            var creature = SceneObjectFactory.Create(1, "object/creature/player/shared_human_female.iff");
            registry.Add(creature);
            long revision = registry.Revision;
            Assert.That(registry.TryGet(1, out var found), Is.True);
            Assert.That(found, Is.SameAs(creature));
            Assert.That(registry.TryGet<CreatureSceneObject>(1, out var typed), Is.True);
            Assert.That(typed, Is.SameAs(creature));
            Assert.That(registry.TryGet<CreatureSceneObject>(2, out var missing), Is.False);
            Assert.That(missing, Is.Null);
            Assert.That(registry.TryGet(2, out var absent), Is.False);
            Assert.That(absent, Is.Null);
            registry.Add(new SceneObject { SwgId = 2 });
            Assert.That(registry.TryGet<SpatialSceneObject>(2, out var wrongType), Is.False);
            Assert.That(wrongType, Is.Null);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++)
            {
                registry.TryGet(1, out _);
                registry.TryGet<CreatureSceneObject>(1, out _);
            }

            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero);
            Assert.That(registry.Revision, Is.EqualTo(revision + 1), "Lookups must not publish changes");
        }

        [Test]
        public void NonspatialBaselineReplacesAnUnknownSpatialPlaceholder()
        {
            using var world = GameServicesForTests.CreateReplication(GameServicesForTests.CreateState(out var currentPlayer), currentPlayer);
            world.Commit(1, new MovementUpdate(position: Vector3.one));
            world.ApplyPending();
            Assert.That(world.SceneObjects.TryGet(1, out var foundObject1), Is.True);
            Assert.That(foundObject1, Is.TypeOf<SpatialSceneObject>());
            world.Commit(1, new ReplicationLifecycleUpdate(objectType: (ReplicatedObjectType)(0x4d49534f)));
            world.ApplyPending();
            Assert.That(world.SceneObjects.TryGet(1, out var foundObject2), Is.True);
            Assert.That(foundObject2, Is.TypeOf<MissionSceneObject>());
            Assert.That(world.SceneObjects.TryGet(1, out var mission), Is.True);
            world.Commit(1, new ReplicationLifecycleUpdate(objectType: (ReplicatedObjectType)(0x4d49534f)));
            world.ApplyPending();
            Assert.That(world.SceneObjects.TryGet(1, out var foundObject3), Is.True);
            Assert.That(foundObject3, Is.SameAs(mission));
        }

        [Test]
        public void LateTypeMetadataPreservesCreateFieldsAndSubsequentReferences()
        {
            using var world = GameServicesForTests.CreateReplication(GameServicesForTests.CreateState(out var currentPlayer), currentPlayer);
            world.Commit(10, new ReplicationLifecycleUpdate(templateCrc: 123), initialMovement: new MovementUpdate(position: new Vector3(1, 2, 3)));
            world.ApplyPending();
            Assert.That(world.SceneObjects.TryGet(10, out var foundObject4), Is.True);
            Assert.That(foundObject4.GetType(), Is.EqualTo(typeof(SpatialSceneObject)));
            world.Commit(10, new ReplicationLifecycleUpdate(objectType: (ReplicatedObjectType)(0x4352454f)), objectUpdate: new SceneObjectUpdate { Name = "Creature" });
            world.ApplyPending();
            Assert.That(world.SceneObjects.TryGet<CreatureSceneObject>(10, out var creature), Is.True);
            Assert.That(creature.Position, Is.EqualTo(new Vector3(1, 2, 3)));
            world.Commit(10, new ContainmentUpdate(parentSwgId: 20));
            world.Commit(10, new ReplicationLifecycleUpdate(objectType: (ReplicatedObjectType)(0x54414e4f)));
            world.ApplyPending();
            Assert.That(world.SceneObjects.TryGet(10, out var foundObject5), Is.True);
            Assert.That(foundObject5, Is.SameAs(creature), "A generic tangible view must not downgrade a creature");
            Assert.That(creature.ParentSwgId, Is.EqualTo(new SwgId(20)));
        }
    }
}
