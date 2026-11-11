using Swg.Client.Core;
using Swg.Client.Core.World.SceneObjects;
using System;
using System.IO;
using NUnit.Framework;
using Swg.Client.Core.Assets;
using Swg.Client.Core.Assets.Tre;
using Swg.Client.Core.Configuration;
using Swg.Client.Core.Networking.Game;
using Swg.Client.Core.Networking.Game.Replication;
using Swg.Client.Core.Networking.Game.Packets.Scene;
using Swg.Client.Core.World;
using Swg.Client.Runtime.World;
using UnityEngine;

namespace Swg.Client.Tests.Editor
{
    [Category("NativeAssets")]
    public sealed class WorldAppearanceChecks
    {
        const string Human = "object/creature/player/shared_human_female.iff";
        const string Hair = "object/tangible/hair/human/shared_hair_human_female_s01.iff";
        WorldState data;
        [OneTimeSetUp]
        public void LoadTemplateNames()
        {
            var settings = ClientSettings.From(Path.Combine(Directory.GetParent(Application.dataPath).FullName, "client.ini"));
            using var loader = new TreAssetLoader(settings.TreDirectory, settings.TreArchivePriorities);
            data = new WorldState(new SceneObjectRegistry())
            {
                Templates = new ObjectTemplateCatalog(loader)
            };
            Assert.That(data.Templates.Resolve(ObjectTemplateNames.TemplateCrc(Hair)), Is.EqualTo(Hair));
        }

        [Test]
        public void LateTemplateCatalogRefinesTheModelOutsideTheRegistryAndPreservesGraphState()
        {
            using var world = GameServicesForTests.CreateReplication(new WorldState(new SceneObjectRegistry()));
            world.Commit(1, new GameObjectUpdate { TemplateCrc = ObjectTemplateNames.TemplateCrc(Human), Position = new Vector3(1, 2, 3), Customization = new byte[] { 1, 0, 255, 3 }, Name = "Player" });
            world.ApplyPending();
            Assert.That(world.SceneObjects.TryGet(1, out var previous), Is.True);
            Assert.That(previous, Is.TypeOf<TangibleSceneObject>());
            var child = new TangibleSceneObject
            {
                SwgId = 2,
                ParentSwgId = 1
            };
            world.SceneObjects.Add(child);
            int notifications = 0;
            world.SceneObjects.Subscribe(1, changes => notifications++);
            world.SetTemplates(data.Templates);
            Assert.That(world.SceneObjects.TryGet<CreatureSceneObject>(1, out var creature), Is.True);
            Assert.That(creature.TemplatePath, Is.EqualTo(Human));
            Assert.That(creature.Position, Is.EqualTo(new Vector3(1, 2, 3)));
            Assert.That(creature.Name, Is.EqualTo("Player"));
            Assert.That(creature.Customization, Is.EqualTo(new byte[] { 1, 0, 255, 3 }));
            Assert.That(child.Parent, Is.SameAs(creature));
            Assert.That(creature.Children, Does.Contain(child));
            Assert.That(previous.Children, Is.Empty);
            Assert.That(notifications, Is.GreaterThan(0));
            world.Commit(3, new GameObjectUpdate { TemplateCrc = ObjectTemplateNames.TemplateCrc(Human), Position = Vector3.one });
            world.ApplyPending();
            Assert.That(world.SceneObjects.TryGet<CreatureSceneObject>(3, out var createdWithCatalog), Is.True);
            Assert.That(createdWithCatalog.Position, Is.EqualTo(Vector3.one));
        }

        static CreatureSceneObject Player() => new CreatureSceneObject
        {
            SwgId = 1,
            TemplatePath = Human,
        };
        static GameWorldSnapshot Snapshot(params SceneObject[] objects) => new GameWorldSnapshot(new CmdStartScenePacket(1, "terrain/corellia.trn", Vector3.zero, Human, 0), true, true, 1, objects);
        [Test]
        public void MovementAndNamesDoNotInvalidateModelIdentity()
        {
            var player = Player();
            var original = WorldAppearance.Capture(data, Snapshot(player))[1];
            player.Position = new Vector3(100, 20, 50);
            player.Rotation = Quaternion.Euler(0, 60, 0);
            player.Name = "Updated name";
            player.Scale = 1.5f;
            Assert.That(WorldAppearance.Capture(data, Snapshot(player))[1].Key, Is.EqualTo(original.Key));
            player.Customization = new byte[]
            {
                1,
                1,
                1,
                2,
                255,
                3
            };
            Assert.That(WorldAppearance.Capture(data, Snapshot(player))[1].Key, Is.Not.EqualTo(original.Key));
        }

        [Test]
        public void EquipmentArrangementAndChildAppearanceInvalidateTheSameModelKey()
        {
            var player = Player();
            uint hairCrc = ObjectTemplateNames.TemplateCrc(Hair);
            player.Equipment = new[]
            {
                new WorldEquipment(Array.Empty<byte>(), 4, 2, hairCrc)
            };
            var first = WorldAppearance.Capture(data, Snapshot(player))[1];
            player.Equipment = new[]
            {
                new WorldEquipment(Array.Empty<byte>(), 5, 2, hairCrc)
            };
            var second = WorldAppearance.Capture(data, Snapshot(player))[1];
            Assert.That(second.Key, Is.Not.EqualTo(first.Key));
            Assert.That(new WorldAppearance(second.TemplatePath, second.Character).Key, Is.EqualTo(second.Key));
            player.Equipment = new[] { new WorldEquipment(Array.Empty<byte>(), 4, 2, hairCrc) };
            var child = new TangibleSceneObject
            {
                SwgId = 2,
                ParentSwgId = 1,
                TemplatePath = Hair,
                Arrangement = 4
            };
            Assert.That(WorldAppearance.Capture(data, Snapshot(player, child))[1].Key, Is.EqualTo(first.Key));
            child.Customization = new byte[]
            {
                1,
                1,
                1,
                3,
                255,
                3
            };
            Assert.That(WorldAppearance.Capture(data, Snapshot(player, child))[1].Key, Is.Not.EqualTo(first.Key));
            player.Equipment = Array.Empty<WorldEquipment>();
            Assert.That(WorldAppearance.Capture(data, Snapshot(player, child))[1].Character.Wearables, Is.Empty,
                "A lingering containment child must not keep an unequipped item visible.");
        }

        [Test]
        public void EquippedChildrenAreNotRenderedTwiceAndInventoryIsExcluded()
        {
            var player = Player();
            player.Equipment = new[]
            {
                new WorldEquipment(Array.Empty<byte>(), 4, 2, ObjectTemplateNames.TemplateCrc(Hair))
            };
            var child = new TangibleSceneObject
            {
                SwgId = 2,
                ParentSwgId = 1,
                TemplatePath = Hair,
                Arrangement = 4
            };
            var captured = WorldAppearance.Capture(data, Snapshot(child, player));
            Assert.That(captured.Count, Is.EqualTo(1));
            Assert.That(captured[1].Character.Wearables.Count, Is.EqualTo(1));
        }

        [Test]
        public void SnapshotOrderDoesNotChangeAppearanceIdentity()
        {
            var player = Player();
            var hair = new TangibleSceneObject
            {
                SwgId = 3,
                ParentSwgId = 1,
                TemplatePath = Hair,
                Arrangement = 4
            };
            var other = new TangibleSceneObject
            {
                SwgId = 2,
                ParentSwgId = 1,
                TemplatePath = Hair,
                Arrangement = 5
            };
            Assert.That(WorldAppearance.Capture(data, Snapshot(player, hair, other))[1].Key, Is.EqualTo(WorldAppearance.Capture(data, Snapshot(other, player, hair))[1].Key));
        }
    }
}
