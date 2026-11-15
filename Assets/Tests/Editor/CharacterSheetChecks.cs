using Autofac;
using System.Reflection;
using UnityEditor;
using Swg.Client.Runtime.UI.CharacterSheet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Swg.Client.Core;
using Swg.Client.Core.Characters;
using Swg.Client.Core.Configuration;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game;
using Swg.Client.Core.Networking.Game.Replication;
using Swg.Client.Core.Networking.Game.Packets.Player;
using Swg.Client.Core.World;
using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;
using Swg.Client.Runtime.Networking;
using Swg.Client.Runtime.Assets.Tre;
using Swg.Client.Runtime.Localization;

namespace Swg.Client.Tests.Editor
{
    public sealed class CharacterSheetChecks
    {
        [Test]
        public void ConsumptionBaselineAndDeltasUpdateTheSharedSheet()
        {
            var playerRegistry = new SceneObjectRegistry();
            var currentPlayer = new CurrentPlayer(playerRegistry);
            var state = new WorldState(playerRegistry);
            var notifications = new List<CharacterSheetChange>();
            currentPlayer.CharacterSheet.Changed += notifications.Add;
            var baseline = Bytes(writer =>
            {
                writer.Write(new byte[8 + 16 + 8 + 8 + 8 + 8]);
                writer.Write(0); // language
                writer.Write(65); writer.Write(100); writer.Write(25); writer.Write(50);
                writer.Write(new byte[20]);
            });
            PlayerView9.ReadBaseline(new ByteReader(baseline)).Details(currentPlayer);
            Assert.That(currentPlayer.CharacterSheet.FoodFullness, Is.EqualTo(.65f));
            Assert.That(currentPlayer.CharacterSheet.DrinkFullness, Is.EqualTo(.5f));
            var delta = Bytes(writer =>
            {
                writer.Write((ushort)4);
                writer.Write((ushort)10); writer.Write(80);
                writer.Write((ushort)11); writer.Write(125);
                writer.Write((ushort)12); writer.Write(40);
                writer.Write((ushort)13); writer.Write(80);
            });
            Assert.That(PlayerView9.TryReadDelta(new ByteReader(delta), out var update), Is.True);
            update.Details(currentPlayer);
            Assert.That(notifications, Is.EqualTo(new[] { CharacterSheetChange.Status, CharacterSheetChange.Status }));
            Assert.That(currentPlayer.CharacterSheet.FoodFullness, Is.EqualTo(.64f));
            Assert.That(currentPlayer.CharacterSheet.DrinkFullness, Is.EqualTo(.5f));
        }

        [Test]
        public void BaseAttributesAndEncumbranceSurviveHamAndWoundUpdates()
        {
            var creature = new CreatureSceneObject();
            var values = new[] { 700, 400, 400, 900, 500, 400, 1000, 500, 600 };
            creature.Apply(new CreatureSceneObjectUpdate { Ham = new HamPoolUpdate { Base = values, Maximum = new[] { 700, 390, 390, 900, 480, 380, 1000, 470, 570 }, Current = values, Encumbrances = new[] { 10, 20, 30 } }, BattleFatigue = 100 });
            creature.Apply(new CreatureSceneObjectUpdate { Ham = new HamPoolUpdate { Wounds = new[] { 699, 2, 2, 701, 1, 1, 100, 0, 0 } } });
            Assert.That(creature.Ham.Health.Base, Is.EqualTo(700));
            Assert.That(creature.Ham.Health.AvailableMaximum, Is.EqualTo(1));
            Assert.That(creature.Ham.Quickness.Encumbrance, Is.EqualTo(20));
            Assert.That(creature.Ham.Focus.Encumbrance, Is.EqualTo(30));
            Assert.That(creature.Ham.Strength.Encumbrance, Is.EqualTo(10));
            Assert.That(creature.BattleFatigue, Is.EqualTo(100));
        }

        [Test]
        public void ProfileAndMigrationRepliesAreAppliedThroughTheMainThreadQueue()
        {
            var playerRegistry = new SceneObjectRegistry();
            var currentPlayer = new CurrentPlayer(playerRegistry);
            var state = new WorldState(playerRegistry);
            currentPlayer.BeginScene(42);
            state.BeginScene(Vector3.zero, "terrain/corellia.trn");
            var replication = GameServicesForTests.CreateReplication(state, currentPlayer);
            using var scope = GameServicesForTests.Create(replication, currentPlayer);
            var router = new GamePacketRouter(scope);
            var notifications = new List<CharacterSheetChange>();
            currentPlayer.CharacterSheet.Changed += notifications.Add;
            try
            {
                router.Accept(Bytes(writer =>
                {
                    writer.Write((ushort)13); writer.Write(CharacterSheetResponsePacket.Opcode);
                    writer.Write(0); writer.Write(0);
                    writer.Write(new byte[12]); Ascii(writer, "corellia");
                    writer.Write(new byte[12]); Ascii(writer, "naboo");
                    writer.Write(new byte[12]); Ascii(writer, "tatooine");
                    Unicode(writer, "Partner"); writer.Write(175); writer.Write(0u); writer.Write(0u);
                }));
                router.Accept(Bytes(writer =>
                {
                    writer.Write((ushort)9); writer.Write(StatMigrationTargetsPacket.Opcode);
                    foreach (int value in new[] { 700, 400, 400, 900, 500, 400, 1000, 500, 600 }) writer.Write(value);
                    writer.Write(0);
                }));
                Assert.That(currentPlayer.CharacterSheet.Spouse, Is.Empty);
                replication.ApplyPending();
                Assert.That(currentPlayer.CharacterSheet.Spouse, Is.EqualTo("Partner"));
                Assert.That(currentPlayer.CharacterSheet.LotsRemaining, Is.EqualTo(175));
                Assert.That(currentPlayer.CharacterSheet.BankPlanet, Is.EqualTo("naboo"));
                Assert.That(currentPlayer.CharacterSheet.MigrationTargets.Mind, Is.EqualTo(1000));
                Assert.That(currentPlayer.CharacterSheet.MigrationBudget, Is.EqualTo(5400));
                Assert.That(notifications, Is.EqualTo(new[] { CharacterSheetChange.Status | CharacterSheetChange.Personal, CharacterSheetChange.Migration }));
            }
            finally { UnityEngine.Object.DestroyImmediate(replication.gameObject); }
        }

        [UnityTest, Category("NativeAssets")]
        public IEnumerator MigrationUsesNativeSpeciesLimitsAndOnlySubmitsValidTargets()
        {
            var settings = ClientSettings.From(Path.Combine(Directory.GetParent(Application.dataPath).FullName, "client.ini"));
            using var assets = new TreRuntimeAssets(settings);
            var playerRegistry = new SceneObjectRegistry();
            var currentPlayer = new CurrentPlayer(playerRegistry);
            var state = new WorldState(playerRegistry);
            currentPlayer.BeginScene(42);
            state.BeginScene(Vector3.zero, "terrain/corellia.trn");
            state.SceneObjects.Add(new CreatureSceneObject { SwgId = 42, TemplatePath = "object/creature/player/shared_human_female.iff" });
            var sent = new List<string>();
            var sheet = new ClientCharacterSheet(state, assets, new StringTableLocalizer(assets), (command, arguments, target) => sent.Add(command + ":" + arguments), packet => { }, currentPlayer);
            yield return new WaitUntil(() => sheet.Loading.IsCompleted);
            Assert.That(sheet.Loading.IsFaulted, Is.False);
            var targets = HamAttributes.FromArray(new[] { 700, 400, 400, 900, 500, 400, 1000, 500, 600 });
            currentPlayer.CharacterSheet.MigrationTargets = targets;
            currentPlayer.CharacterSheet.MigrationBudget = targets.Total;
            currentPlayer.CharacterSheet.HasMigrationTargets = true;
            Assert.That(sheet.SaveMigration(targets, out var error), Is.True, error);
            Assert.That(sent.Single(), Is.EqualTo("requestSetStatMigrationData:700 400 400 900 500 400 1000 500 600"));
            targets.Health++;
            Assert.That(sheet.SaveMigration(targets, out error), Is.False);
            Assert.That(sent.Count, Is.EqualTo(1));
            Assert.That(sheet.MigrationLimits(false).Health, Is.GreaterThan(0));
            Assert.That(sheet.MigrationLimits(true).Health, Is.GreaterThanOrEqualTo(700));
        }

        [TestCase(0)]
        [TestCase(100)]
        public void ArmorPenaltiesAffectSecondaryAttributesWithoutBecomingGenericModifiers(int strengthBuff)
        {
            var baseline = new[] { 700, 400, 400, 900, 500, 400, 1000, 500, 600 };
            // Core3 PlayerManager::fixHAM: maximum = base + modifier - secondary armor penalty.
            var maximum = new[] { 700, 350 + strengthBuff, 350, 900, 425, 325, 1000, 475, 575 };
            var creature = new CreatureSceneObject();
            creature.Apply(new CreatureSceneObjectUpdate { Ham = new HamPoolUpdate { Base = baseline, Maximum = maximum, Current = maximum, Encumbrances = new[] { 50, 75, 25 } } });
            var attributes = ClientCharacterSheet.Attributes(creature.Ham);
            for (int i = 0; i < attributes.Length; i++)
            {
                int penalty = i % 3 == 0 ? 0 : new[] { 50, 75, 25 }[i / 3];
                Assert.That(attributes[i].Encumbrance, Is.EqualTo(penalty));
                Assert.That(attributes[i].Modifiers, Is.EqualTo(i == 1 ? strengthBuff : 0));
                Assert.That(attributes[i].Base + attributes[i].Modifiers - attributes[i].Encumbrance, Is.EqualTo(attributes[i].Maximum));
            }
            var delta = IntVectorDelta.ReadFrom(new ByteReader(Bytes(writer =>
            {
                writer.Write(1u); writer.Write(2u);
                writer.Write((byte)2); writer.Write((ushort)0); writer.Write(0);
            })));
            maximum[1] = 400 + strengthBuff;
            maximum[2] = 400;
            creature.Apply(new CreatureSceneObjectUpdate { Ham = new HamPoolUpdate { EncumbrancesDelta = delta, Maximum = maximum } });
            Assert.That(creature.Ham.Strength.Encumbrance, Is.Zero);
            Assert.That(creature.Ham.Constitution.Encumbrance, Is.Zero);
            Assert.That(creature.Ham.Strength.Modifiers, Is.EqualTo(strengthBuff));
            Assert.That(creature.Ham.Quickness.Encumbrance, Is.EqualTo(75), "An encumbrance delta must retain the other groups.");
            Assert.That(creature.Ham.Focus.Encumbrance, Is.EqualTo(25));
        }

        [UnityTest, Category("NativeAssets")]
        public IEnumerator ReopenedMigrationWaitsForItsDelayedFreshReplyBeforeDisplayingOrSavingTargets()
        {
            using var assets = new TreRuntimeAssets(Settings());
            var state = SheetState(out var currentPlayer);
            var sent = new List<string>();
            var strings = new StringTableLocalizer(assets);
            var sheet = new ClientCharacterSheet(state, assets, strings, (command, arguments, target) => sent.Add(command + ":" + arguments), packet => { }, currentPlayer);
            yield return new WaitUntil(() => sheet.Loading.IsCompleted);
            Assert.That(sheet.Loading.IsCompletedSuccessfully, Is.True);
            using var services = ManagedObjectTestScope.Use(state.SceneObjects, builder =>
            {
                builder.RegisterInstance(state);
                builder.RegisterInstance(currentPlayer);
                builder.RegisterInstance(sheet);
            });
            var replication = GameServicesForTests.CreateReplication(state, currentPlayer);
            using var routing = GameServicesForTests.Create(replication, currentPlayer);
            var router = new GamePacketRouter(routing);
            var root = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/CharacterSheet/StatMigration.prefab"));
            var window = root.GetComponent<StatMigrationWindow>();
            try
            {
                Invoke(window, "Awake");
                ReceiveMigration(router, replication, new[] { 700, 400, 400, 900, 500, 400, 1000, 500, 600 });
                Invoke(window, "OnEnable");
                Assert.That(sheet.Data.HasMigrationTargets, Is.False);
                Assert.That(window.save.interactable, Is.False);
                Assert.That(window.rows.All(row => !row.slider.interactable && row.target.text == "--"), Is.True);
                window.Save();
                yield return null;
                var firstFresh = new[] { 690, 410, 400, 900, 500, 400, 1000, 500, 600 };
                ReceiveMigration(router, replication, firstFresh);
                Assert.That(window.rows[0].target.text, Is.EqualTo("690"));
                Assert.That(window.save.interactable, Is.True);
                window.Save();
                Assert.That(sent.Count(command => command.StartsWith("requestSetStatMigrationData:")), Is.EqualTo(1));
                Invoke(window, "OnDisable");
                root.SetActive(true);
                Invoke(window, "OnEnable");
                yield return null;
                currentPlayer.CharacterSheet.Update(data => data.Spouse = "Unrelated reply", CharacterSheetChange.Personal);
                yield return null;
                Assert.That(window.rows[0].target.text, Is.EqualTo("--"), "Cached targets and unrelated replies must not initialize a reopened popup.");
                Assert.That(window.save.interactable, Is.False);
                Assert.That(sheet.SaveMigration(HamAttributes.FromArray(firstFresh), out _), Is.False);
                window.Save();
                Assert.That(sent.Count(command => command.StartsWith("requestSetStatMigrationData:")), Is.EqualTo(1));
                var secondFresh = new[] { 680, 420, 400, 900, 500, 400, 1000, 500, 600 };
                ReceiveMigration(router, replication, secondFresh);
                Assert.That(window.rows[0].target.text, Is.EqualTo("680"));
                Assert.That(window.rows[1].target.text, Is.EqualTo("420"));
                Assert.That(window.rows.All(row => row.slider.interactable), Is.True);
                window.Save();
                Assert.That(sent.Last(), Is.EqualTo("requestSetStatMigrationData:" + string.Join(" ", secondFresh)));
                Assert.That(sent.Count(command => command.StartsWith("requestStatMigrationData:")), Is.EqualTo(2));
                Invoke(window, "OnDisable");
                root.SetActive(true);
                Invoke(window, "OnEnable"); // A request is now in flight.
                Invoke(window, "OnDisable");
                root.SetActive(false);
                root.SetActive(true);
                Invoke(window, "OnEnable"); // Reopening before that reply must not accept it.
                Assert.That(sent.Count(command => command.StartsWith("requestStatMigrationData:")), Is.EqualTo(3), "Requests stay serialized because the native response is uncorrelated.");
                ReceiveMigration(router, replication, new[] { 660, 440, 400, 900, 500, 400, 1000, 500, 600 });
                Assert.That(window.rows[0].target.text, Is.EqualTo("--"), "The older in-flight reply cannot initialize the current opening.");
                Assert.That(window.save.interactable, Is.False);
                Assert.That(sheet.Data.HasMigrationTargets, Is.False);
                Assert.That(sent.Count(command => command.StartsWith("requestStatMigrationData:")), Is.EqualTo(4));
                ReceiveMigration(router, replication, new[] { 650, 450, 400, 900, 500, 400, 1000, 500, 600 });
                Assert.That(window.rows[0].target.text, Is.EqualTo("650"));
                Assert.That(window.save.interactable, Is.True);
            }
            finally
            {
                Invoke(window, "OnDisable");
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(replication.gameObject);
            }
        }

        [UnityTest, Category("NativeAssets")]
        public IEnumerator SheetIgnoresMovementAndRefreshesOnlyRelevantVisibleSections()
        {
            using var assets = new TreRuntimeAssets(Settings());
            var state = SheetState(out var currentPlayer);
            var strings = new StringTableLocalizer(assets);
            var names = new SceneObjectNameResolver(assets, strings);
            var sheet = new ClientCharacterSheet(state, assets, strings, (command, arguments, target) => { }, packet => { }, currentPlayer);
            yield return new WaitUntil(() => sheet.Loading.IsCompleted);
            Assert.That(sheet.Loading.IsCompletedSuccessfully, Is.True);
            using var services = ManagedObjectTestScope.Use(state.SceneObjects, builder =>
            {
                builder.RegisterInstance(state);
                builder.RegisterInstance(currentPlayer);
                builder.RegisterInstance(sheet);
                builder.RegisterInstance(names);
            });
            var root = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/CharacterSheet/CharacterSheet.prefab"));
            var window = root.GetComponent<CharacterSheetWindow>();
            try
            {
                Invoke(window, "Awake");
                Invoke(window, "OnEnable");
                var loading = (IEnumerator)Invoke(window, "LoadMetadata");
                while (loading.MoveNext()) yield return loading.Current;
                Invoke(window, "LateUpdate");
                window.attributes[0].value.text = "unchanged status";
                window.badges.text = "hidden badges";
                window.married.text = "hidden personal";
                state.SceneObjects.TryGet<CreatureSceneObject>(currentPlayer.SwgId, out var player);
                state.SceneObjects.NotifyChanged(player, SceneObjectChange.Transform | SceneObjectChange.Target | SceneObjectChange.Posture | SceneObjectChange.Inventory);
                Invoke(window, "LateUpdate");
                Assert.That(window.attributes[0].value.text, Is.EqualTo("unchanged status"));
                Assert.That(window.badges.text, Is.EqualTo("hidden badges"));
                player.Apply(new CreatureSceneObjectUpdate { BattleFatigue = 25 });
                state.SceneObjects.NotifyChanged(player, SceneObjectChange.Vitals);
                Invoke(window, "LateUpdate");
                Assert.That(window.fatigue.text, Is.EqualTo("25"));
                Assert.That(window.attributes[0].value.text, Is.EqualTo("700 / 700"));
                Assert.That(window.badges.text, Is.EqualTo("hidden badges"));
                currentPlayer.CharacterSheet.Update(data => data.Spouse = "Partner", CharacterSheetChange.Personal);
                Invoke(window, "LateUpdate");
                Assert.That(window.married.text, Is.EqualTo("hidden personal"));
                window.SelectTab(true);
                Invoke(window, "LateUpdate");
                Assert.That(window.married.text, Is.EqualTo("Partner"));
                Assert.That(window.badges.text, Is.Not.EqualTo("hidden badges"));
                window.badges.text = "unchanged badges";
                window.lots.text = "unchanged profile";
                state.SceneObjects.NotifyChanged(player, SceneObjectChange.Transform | SceneObjectChange.Vitals);
                currentPlayer.CharacterSheet.Update(data => { data.Food = 75; data.FoodMaximum = 100; }, CharacterSheetChange.Status);
                Invoke(window, "LateUpdate");
                Assert.That(window.food.Fill, Is.Zero, "Status controls stay deferred while Personal is shown.");
                Assert.That(window.badges.text, Is.EqualTo("unchanged badges"));
                Assert.That(window.lots.text, Is.EqualTo("unchanged profile"));
                currentPlayer.CharacterSheet.Update(data => data.Biography = "Updated biography", CharacterSheetChange.Personal);
                Invoke(window, "LateUpdate");
                Assert.That(window.biography.text, Does.Contain("Updated biography"));
                Assert.That(window.badges.text, Is.EqualTo("unchanged badges"));
                window.lots.text = "unchanged profile";
                currentPlayer.CharacterSheet.Update(data => data.BadgeMasks = new uint[] { 1 }, CharacterSheetChange.Badges);
                Invoke(window, "LateUpdate");
                Assert.That(window.badges.text, Is.Not.EqualTo("unchanged badges"));
                Assert.That(window.lots.text, Is.EqualTo("unchanged profile"));
                player.Name = "Renamed player";
                state.SceneObjects.NotifyChanged(player, SceneObjectChange.Appearance);
                Invoke(window, "LateUpdate");
                Assert.That(window.title.text, Is.EqualTo("RENAMED PLAYER"));
                window.SelectTab(false);
                Invoke(window, "LateUpdate");
                Assert.That(window.food.Fill, Is.EqualTo(.75f));
            }
            finally
            {
                Invoke(window, "OnDisable");
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        static ClientSettings Settings() => ClientSettings.From(Path.Combine(Directory.GetParent(Application.dataPath).FullName, "client.ini"));
        static WorldState SheetState(out CurrentPlayer currentPlayer)
        {
            var playerRegistry = new SceneObjectRegistry();
            currentPlayer = new CurrentPlayer(playerRegistry);
            var state = new WorldState(playerRegistry);
            currentPlayer.BeginScene(42);
            state.BeginScene(Vector3.zero, "terrain/corellia.trn");
            var player = new CreatureSceneObject { SwgId = 42, Name = "Misty", TemplatePath = "object/creature/player/shared_human_female.iff" };
            var values = new[] { 700, 400, 400, 900, 500, 400, 1000, 500, 600 };
            player.Apply(new CreatureSceneObjectUpdate { Ham = new HamPoolUpdate { Base = values, Maximum = values, Current = values } });
            state.SceneObjects.Add(player);
            return state;
        }
        static object Invoke(object instance, string method) => instance.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(instance, null);
        static void ReceiveMigration(GamePacketRouter router, GameReplication replication, int[] values)
        {
            router.Accept(Bytes(writer =>
            {
                writer.Write((ushort)9); writer.Write(StatMigrationTargetsPacket.Opcode);
                foreach (int value in values) writer.Write(value);
                writer.Write(0);
            }));
            replication.ApplyPending();
        }


        static byte[] Bytes(Action<BinaryWriter> write) { using var stream = new MemoryStream(); using var writer = new BinaryWriter(stream); write(writer); return stream.ToArray(); }
        static void Ascii(BinaryWriter writer, string value) { var bytes = Encoding.ASCII.GetBytes(value); writer.Write((ushort)bytes.Length); writer.Write(bytes); }
        static void Unicode(BinaryWriter writer, string value) { writer.Write(value.Length); writer.Write(Encoding.Unicode.GetBytes(value)); }
    }
}
