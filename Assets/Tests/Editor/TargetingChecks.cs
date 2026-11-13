using Swg.Client.Core.Assets.Appearances;
using System.Collections.Generic;
using System.IO;
using Swg.Client.Core.Assets;
using Swg.Client.Core.Assets.Tre;
using Swg.Client.Runtime.UI.Targeting;
using Swg.Client.Runtime.World;
using Swg.Client.Runtime.Localization;
using Swg.Client.Runtime.Assets.Tre;
using Swg.Client.Core.Configuration;
using UnityEditor;
using NUnit.Framework;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets;
using Swg.Client.Core.Networking.Game.Packets.Targeting;
using Swg.Client.Core.Networking.Game.Replication;
using Swg.Client.Core.World;
using Swg.Client.Core.World.SceneObjects;
using Swg.Client.Runtime.World.SceneObjects;
using UnityEngine;
using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;

namespace Swg.Client.Tests.Editor
{
    public sealed class TargetingChecks
    {
        [Test]
        public void SelectionIsSharedIdempotentAndClearsWhenRemoved()
        {
            var registry = new SceneObjectRegistry();
            registry.Add(new SpatialSceneObject { SwgId = 42 });
            registry.Add(new SceneObject { SwgId = 43 });
            var sent = new List<TargetUpdatePacket>();
            using var targeting = new ClientTargeting(new WorldState(registry), sent.Add);
            targeting.Select(43); // Intangible objects cannot be world targets.
            Assert.That(targeting.SelectedSwgId.IsNone, Is.True);
            targeting.Select(42);
            targeting.Select(42);
            targeting.Hover(42);
            Assert.That(targeting.SelectedSwgId.Value, Is.EqualTo(42));
            Assert.That(sent.Count, Is.EqualTo(1));
            Assert.That(sent[0].TargetSwgId.Value, Is.EqualTo(42));
            registry.Remove(42);
            Assert.That(targeting.SelectedSwgId.IsNone, Is.True);
            Assert.That(targeting.HoveredSwgId.IsNone, Is.True);
            Assert.That(sent.Count, Is.EqualTo(2));
            Assert.That(sent[1].TargetSwgId.IsNone, Is.True);
        }

        [Test]
        public void ReplicatedSelectionFollowsThePlayerWithoutEchoingRequests()
        {
            var registry = new SceneObjectRegistry();
            var state = new WorldState(registry);
            int requests = 0;
            using var targeting = new ClientTargeting(state, _ => requests++);
            state.BeginScene(7, Vector3.zero, "terrain/test.trn");
            var player = new CreatureSceneObject { SwgId = 7, TargetSwgId = 42 };
            registry.Add(player);
            Assert.That(targeting.SelectedSwgId.Value, Is.EqualTo(42));
            player.TargetSwgId = 43;
            registry.NotifyChanged(player, SceneObjectChange.Target);
            Assert.That(targeting.SelectedSwgId.Value, Is.EqualTo(43));
            Assert.That(requests, Is.Zero);
            state.BeginScene(8, Vector3.zero, "terrain/other.trn");
            registry.Add(new CreatureSceneObject { SwgId = 8, TargetSwgId = 44 });
            Assert.That(targeting.SelectedSwgId.Value, Is.EqualTo(44));
            targeting.Dispose();
            registry.TryGet<CreatureSceneObject>(8, out var nextPlayer);
            nextPlayer.TargetSwgId = 45;
            registry.NotifyChanged(nextPlayer, SceneObjectChange.Target);
            Assert.That(targeting.SelectedSwgId.Value, Is.EqualTo(44));
        }

        [Test]
        public void LocalSelectionUpdatesThePlayerAndSendsOnlyChangedTargets()
        {
            var registry = new SceneObjectRegistry();
            var state = new WorldState(registry);
            var sent = new List<TargetUpdatePacket>();
            using var targeting = new ClientTargeting(state, sent.Add);
            state.BeginScene(7, Vector3.zero, "terrain/test.trn");
            var player = new CreatureSceneObject { SwgId = 7 };
            registry.Add(player);
            registry.Add(new SpatialSceneObject { SwgId = 42 });
            targeting.Hover(42);
            Assert.That(sent, Is.Empty);
            targeting.Select(42);
            targeting.Select(42);
            Assert.That(player.TargetSwgId.Value, Is.EqualTo(42));
            Assert.That(sent.Count, Is.EqualTo(1));
            Assert.That(sent[0].PlayerSwgId.Value, Is.EqualTo(7));
            Assert.That(sent[0].TargetSwgId.Value, Is.EqualTo(42));
            registry.Remove(42);
            Assert.That(player.TargetSwgId.IsNone, Is.True);
            Assert.That(sent.Count, Is.EqualTo(2));
            Assert.That(sent[1].TargetSwgId.IsNone, Is.True);
        }

        [Test]
        public void CreatureExposesNamedHamValuesAndWoundCapacity()
        {
            var creature = new CreatureSceneObject
            {
                Ham = new HamPool
                {
                    Health = new HamElement(current: 750, maximum: 1000, wounds: 100),
                    Action = new HamElement(current: 500, maximum: 1000, wounds: 200),
                    Mind = new HamElement(current: 900, maximum: 1000, wounds: 0)
                }
            };
            Assert.That(creature.Ham.HasAnAttackableElement, Is.True);
            Assert.That(creature.Ham.Health.Current, Is.EqualTo(750));
            Assert.That(creature.Ham.Action.Current, Is.EqualTo(500));
            Assert.That(creature.Ham.Mind.Current, Is.EqualTo(900));
            Assert.That(creature.Ham.Health.AvailableMaximum, Is.EqualTo(900));
            Assert.That(creature.Ham.Action.CapacityFraction, Is.EqualTo(.8f));
            Assert.That(creature.Ham.Action.CurrentFraction, Is.EqualTo(.5f));
            creature.Ham.Health = new HamElement(current: 750, maximum: 1000, wounds: 0);
            Assert.That(creature.Ham.Health.Wounds, Is.Zero);
            Assert.That(creature.Ham.Health.CapacityFraction, Is.EqualTo(1));
            Assert.That(new CreatureSceneObject().Ham.HasAnAttackableElement, Is.False);
            Assert.That(new CreatureSceneObject().Ham.Health.CurrentFraction, Is.Zero);
        }

        [Test]
        public void ReplicationMapsAllNineAttributesWithoutExposingWireVectors()
        {
            var creature = new CreatureSceneObject();
            creature.Apply(new CreatureSceneObjectUpdate { Ham = new[] { 10, 11, 12, 13, 14, 15, 16, 17, 18 }, MaxHam = new[] { 100, 101, 102, 103, 104, 105, 106, 107, 108 }, Wounds = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 } });
            var ham = creature.Ham;
            var named = new[] { ham.Health, ham.Strength, ham.Constitution, ham.Action, ham.Quickness, ham.Stamina, ham.Mind, ham.Focus, ham.Willpower };
            for (int i = 0; i < named.Length; i++)
            {
                Assert.That(named[i].Current, Is.EqualTo(10 + i));
                Assert.That(named[i].Maximum, Is.EqualTo(100 + i));
                Assert.That(named[i].Wounds, Is.EqualTo(i));
            }
            creature.Apply(new CreatureSceneObjectUpdate { Ham = new[] { 20, 21, 22, 23, 24, 25, 26, 27, 28 } });
            Assert.That(creature.Ham.Constitution.Current, Is.EqualTo(22));
            Assert.That(creature.Ham.Constitution.Maximum, Is.EqualTo(102));
            Assert.That(creature.Ham.Constitution.Wounds, Is.EqualTo(2));
        }

        [Test]
        public void CreatureSnapshotOwnsItsNamedHamValues()
        {
            var creature = new CreatureSceneObject
            {
                Ham = new HamPool
                {
                    Health = new HamElement(current: 750, maximum: 1000, wounds: 100),
                    Constitution = new HamElement(current: 200, maximum: 300, wounds: 10)
                }
            };
            var copy = (CreatureSceneObject)creature.Copy();
            copy.Ham.Health = new HamElement(1, 2, 0);
            copy.Ham.Constitution = new HamElement(3, 4, 0);
            Assert.That(creature.Ham.Health.Current, Is.EqualTo(750));
            Assert.That(creature.Ham.Constitution.Current, Is.EqualTo(200));
        }

        [Test]
        public void SharedNameResolverHonorsRenamesAndNativeTemplateNames()
        {
            var settings = ClientSettings.From(Path.Combine(Directory.GetParent(Application.dataPath).FullName, "client.ini"));
            using var assets = new TreRuntimeAssets(settings);
            assets.Ready.GetAwaiter().GetResult();
            var names = new SceneObjectNameResolver(assets, new StringTableLocalizer(assets));
            var sceneObject = new SpatialSceneObject { TemplatePath = "object/installation/mining_ore/shared_mining_ore_harvester_style_2.iff" };
            Assert.That(names.Resolve(sceneObject), Is.EqualTo("Mineral Mining Installation"));
            sceneObject.Name = "Custom harvester";
            Assert.That(names.Resolve(sceneObject), Is.EqualTo("Custom harvester"));
            sceneObject.Name = sceneObject.TemplatePath;
            Assert.That(names.Resolve(sceneObject), Is.EqualTo("Mineral Mining Installation"));
            Assert.That(names.Resolve(new SceneObject { Name = "@installation_n:ore_mine" }), Is.EqualTo("Mineral Mining Installation"));
            Assert.That(names.Resolve(new SceneObject()), Is.EqualTo("Object"));
        }

        [TestCase(42ul)]
        [TestCase(0ul)]
        public void TargetPacketMatchesCore3ControllerEnvelope(ulong targetSwgId)
        {
            using var bytes = new MemoryStream();
            using var writer = new BinaryWriter(bytes);
            new TargetUpdatePacket(7, targetSwgId).WriteTo(writer);
            var reader = new ByteReader(bytes.ToArray());
            Assert.That(reader.U16(), Is.EqualTo(ObjectControllerPacket.OperandCount));
            Assert.That(reader.U32(), Is.EqualTo(ObjectControllerPacket.Opcode));
            Assert.That(reader.U32(), Is.EqualTo(0x23));
            Assert.That(reader.U32(), Is.EqualTo(0x126));
            Assert.That(reader.U64(), Is.EqualTo(7));
            Assert.That(reader.U32(), Is.Zero);
            Assert.That(reader.U64(), Is.EqualTo(targetSwgId));
            Assert.That(reader.Remaining, Is.Zero);
        }

        [Test]
        public void CreatureBaselineRetainsHamAndTarget()
        {
            using var bytes = new MemoryStream();
            using var writer = new BinaryWriter(bytes);
            writer.Write(0u);
            writer.Write(0u); writer.Write(0u); // Defenders.
            writer.Write((ushort)1);
            writer.Write((ushort)0); writer.Write((ushort)0); // Performance and mood.
            for (int i = 0; i < 4; i++)
                writer.Write(0ul);
            writer.Write(0u);
            writer.Write(42ul);
            writer.Write((byte)0);
            writer.Write(0u); writer.Write(0u);
            void Vector(int value)
            {
                writer.Write(9u); writer.Write(1u);
                for (int i = 0; i < 9; i++)
                    writer.Write(value + i);
            }
            Vector(500);
            Vector(1000);
            writer.Write(0u); writer.Write(0u); // Equipment.
            writer.Write((ushort)0);
            writer.Write((byte)0);
            var reader = new ByteReader(bytes.ToArray());
            var update = CreatureView6.ReadBaseline(reader);
            ReplicationReader.RequireEnd(reader);
            var creature = new CreatureSceneObject();
            creature.Apply(update);
            Assert.That(creature.Ham.Action.Current, Is.EqualTo(503));
            Assert.That(creature.Ham.Mind.Maximum, Is.EqualTo(1006));
            Assert.That(creature.TargetSwgId.Value, Is.EqualTo(42));
            var copy = (CreatureSceneObjectUpdate)update.CopyForQueue();
            update.Ham[0] = 0;
            Assert.That(copy.Ham[0], Is.EqualTo(500));
        }

        [TestCase(13, 3)]
        [TestCase(14, 3)]
        [TestCase(17, 3)]
        [TestCase(13, 2)]
        [TestCase(14, 2)]
        [TestCase(17, 2)]
        public void VitalsDeltasDoNotInvalidateAppearances(int field, int attributeIndex)
        {
            using var bytes = new MemoryStream();
            using var writer = new BinaryWriter(bytes);
            writer.Write(1u); writer.Write(2u);
            writer.Write((byte)2); writer.Write((ushort)attributeIndex); writer.Write(123);
            var update = new CreatureSceneObjectUpdate();
            var reader = new ByteReader(bytes.ToArray());
            bool handled = field == 17 ? CreatureView3.ReadDeltaField((ushort)field, reader, update) : CreatureView6.ReadDeltaField((ushort)field, reader, update);
            Assert.That(handled, Is.True);
            ReplicationReader.RequireEnd(reader);
            var creature = new CreatureSceneObject();
            creature.Apply(update);
            var pool = attributeIndex == 3 ? creature.Ham.Action : creature.Ham.Constitution;
            Assert.That(field == 13 ? pool.Current : field == 14 ? pool.Maximum : pool.Wounds, Is.EqualTo(123));
            Assert.That(update.Changes, Is.EqualTo(SceneObjectChange.Vitals));
        }

        [Test]
        public void HamWidthsAreRelativeAndOnlyWoundsRemoveCapacity()
        {
            var health = new HamElement(750, 1000, 0);
            var action = new HamElement(500, 900, 180);
            Assert.That(health.RelativeWidth(1000), Is.EqualTo(1));
            Assert.That(action.RelativeWidth(1000), Is.EqualTo(.9f));
            Assert.That(health.CapacityFraction, Is.EqualTo(1));
            Assert.That(health.WoundFraction, Is.Zero);
            Assert.That(action.CapacityFraction, Is.EqualTo(.8f));
            Assert.That(action.WoundFraction, Is.EqualTo(.2f));
            Assert.That(new HamElement(1000, 1000, 100).CurrentFraction, Is.EqualTo(.9f));
        }

        [Test]
        public void NativeTemplatesDistinguishTargetsFromStaticScenery()
        {
            using var assets = new TreAssetLoader(@"D:\SwgEmu\emu\SWGEmu");
            var templates = new ObjectTemplateCatalog(assets);
            Assert.That(templates.IsTargetable("object/creature/player/shared_human_male.iff"), Is.True);
            Assert.That(templates.IsTargetable("object/installation/mining_ore/shared_mining_ore_harvester_style_2.iff"), Is.True);
            Assert.That(templates.IsTargetable("object/static/structure/corellia/shared_corl_imprv_column_s01.iff"), Is.False);
        }

        [Test]
        public void CreatureSelectionUsesUpperBodyWithoutShrinkingThePickVolume()
        {
            var root = new GameObject("Creature pick bounds");
            try
            {
                var target = root.AddComponent<TargetableSceneObject>();
                var appearance = new Bounds(new Vector3(0, 1, 0), new Vector3(1, 2, 1));
                target.Initialize(42, new AppearanceData { BoundsMin = appearance.min, BoundsMax = appearance.max }, true);
                Assert.That(target.LocalBounds, Is.EqualTo(appearance));
                Assert.That(target.SelectionBounds.min.y, Is.EqualTo(1));
                Assert.That(target.SelectionBounds.max.y, Is.EqualTo(2));
                Assert.That(root.GetComponentInChildren<BoxCollider>().size, Is.EqualTo(appearance.size));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void NativeHarvesterTemplateHasTheLocalizedInstallationName()
        {
            using var assets = new TreAssetLoader(@"D:\SwgEmu\emu\SWGEmu");
            Assert.That(ObjectTemplateNames.DisplayName(assets, "object/installation/mining_ore/shared_mining_ore_harvester_style_2.iff"), Is.EqualTo("@installation_n:ore_mine"));
        }

        [Test]
        public void WorldPrefabBindsTargetHudToItsOwnCamera()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/World.prefab");
            var world = prefab.GetComponentInChildren<WorldController>(true);
            var hud = prefab.GetComponentInChildren<TargetHud>(true);
            Assert.That(hud.worldCamera, Is.SameAs(world.orbit.worldCamera));
            foreach (var bar in hud.ham.currentBars)
                Assert.That(bar.GetComponent<TreTextureBinding>().asset, Is.Not.Null);
            foreach (var bar in hud.ham.woundBars)
            {
                Assert.That(bar.color, Is.EqualTo(Color.black));
                Assert.That(bar.fillOrigin, Is.EqualTo((int)UnityEngine.UI.Image.OriginHorizontal.Right));
            }
            Assert.That(hud.postureIcons.Length, Is.EqualTo(15));
            var targeting = world.orbit.worldCamera.GetComponent<WorldTargeting>();
            Assert.That(targeting.enabled, Is.False, "WorldController enables mouse input when the world is revealed.");
            Assert.That(targeting.hoverCursor, Is.Not.Null);
        }

        [Test]
        public void PickBoundsExcludeParticleEffects()
        {
            var root = new GameObject("Target bounds");
            try
            {
                var mesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.DestroyImmediate(mesh.GetComponent<BoxCollider>());
                mesh.transform.SetParent(root.transform, false);
                mesh.transform.localScale = new Vector3(2, 4, 6);
                var effect = new GameObject("Beacon", typeof(ParticleSystem));
                effect.transform.SetParent(root.transform, false);
                effect.transform.localPosition = Vector3.up * 100;
                var target = root.AddComponent<TargetableSceneObject>();
                target.Initialize(42, new AppearanceData { BoundsMin = new Vector3(-1, -2, -3), BoundsMax = new Vector3(1, 2, 3) }, false);
                Assert.That(target.LocalBounds.size, Is.EqualTo(new Vector3(2, 4, 6)));
                Assert.That(root.GetComponentInChildren<BoxCollider>().isTrigger, Is.True);
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
