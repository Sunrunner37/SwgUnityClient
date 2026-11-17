using System.Linq;
using System;
using NUnit.Framework;
using UnityEngine;
using Swg.Client.Core;
using Swg.Client.Core.Characters;
using Swg.Client.Core.Crafting;
using Swg.Client.Core.World;
using Swg.Client.Core.World.SceneObjects;
using Swg.Client.Runtime.Networking;
using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;

namespace Swg.Client.Tests.Editor
{
    public sealed class CurrentPlayerChecks
    {
        [Test]
        public void CreatureAccessAlwaysUsesTheRegistryInstance()
        {
            var registry = new SceneObjectRegistry();
            var player = new CurrentPlayer(registry);
            var world = new WorldState(registry);
            player.BeginScene(42);
            world.BeginScene(Vector3.zero, "terrain/corellia.trn");
            Assert.That(player.Creature, Is.Null);
            var original = new CreatureSceneObject { SwgId = 42, Name = "Original" };
            registry.Add(original);
            Assert.That(player.Creature, Is.SameAs(original));
            var replacement = new CreatureSceneObject { SwgId = 42, Name = "Replacement" };
            registry.Replace(replacement);
            Assert.That(player.Creature, Is.SameAs(replacement));
            registry.Remove(42);
            Assert.That(player.Creature, Is.Null);
        }

        [Test]
        public void SceneStartResetsOwnerProgressWithoutDuplicatingTheCreature()
        {
            var registry = new SceneObjectRegistry();
            var player = new CurrentPlayer(registry);
            var world = new WorldState(registry);
            player.BeginScene(42);
            world.BeginScene(Vector3.zero, "terrain/corellia.trn");
            registry.Add(new CreatureSceneObject { SwgId = 42 });
            var oldSheet = player.CharacterSheet;
            player.UpdateExperience(values => values["music"] = 123);
            player.Commands.Apply(values => values.Add("startMusic"));
            player.UpdateDraftSchematics(values => values.Add(100));
            player.UpdateDraftDetails(100, new DraftSchematicDetails());
            player.ShowWaypointIndicators = false;
            player.BeginScene(99);
            world.BeginScene(Vector3.one, "terrain/tatooine.trn");
            Assert.That(player.SwgId, Is.EqualTo(new SwgId(99)));
            Assert.That(player.Creature, Is.Null);
            Assert.That(player.CharacterSheet, Is.Not.SameAs(oldSheet));
            Assert.That(player.Experience, Is.Empty);
            Assert.That(player.Commands.Abilities, Is.Empty);
            Assert.That(player.DraftSchematics, Is.Empty);
            Assert.That(player.DraftDetails, Is.Empty);
            Assert.That(player.ShowWaypointIndicators, Is.True);
            Assert.That(world.RenderOrigin, Is.EqualTo(Vector3.one));
        }

        [Test]
        public void PersonalAndMissionWaypointsShareThePlayerNotificationLifetime()
        {
            var registry = new SceneObjectRegistry();
            var player = new CurrentPlayer(registry);
            var world = new WorldState(registry);
            player.BeginScene(42);
            world.BeginScene(Vector3.zero, "terrain/corellia.trn");
            int notifications = 0;
            player.WaypointsChanged += () => notifications++;
            player.UpdateWaypoints(values => values[1] = new Waypoint(1, Vector3.zero, world.PlanetCrc, "Home", WaypointColor.Blue, true));
            Assert.That(notifications, Is.EqualTo(1));
            Assert.That(player.NavigationWaypoints.Count(), Is.EqualTo(1));
            player.Dispose();
            registry.Add(new MissionSceneObject { SwgId = 2, Waypoint = new Waypoint(2, Vector3.zero, 0, "Mission", WaypointColor.Orange, true) });
            Assert.That(notifications, Is.EqualTo(1));
        }

        [Test]
        public void ReplicationAppliesPlayerAndCraftingChangesOnTheMainThreadTogether()
        {
            var registry = new SceneObjectRegistry();
            var player = new CurrentPlayer(registry);
            var world = new WorldState(registry);
            var progress = new CraftingProgress();
            var root = new GameObject("Player replication test");
            try
            {
                var replication = root.AddComponent<GameReplication>();
                replication.Initialize(world, player, progress, _ => { });
                replication.UpdatePlayer(owner => owner.CharacterSheet.Update(sheet => sheet.Food = 42, CharacterSheetChange.Status),
                    crafting => crafting.Update(value => value.ExperimentationPoints = 11));
                Assert.That(player.CharacterSheet.Food, Is.Zero);
                Assert.That(progress.ExperimentationPoints, Is.Zero);
                replication.ApplyPending();
                Assert.That(player.CharacterSheet.Food, Is.EqualTo(42));
                Assert.That(progress.ExperimentationPoints, Is.EqualTo(11));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
    }
}
