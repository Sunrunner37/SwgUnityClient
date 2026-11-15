using Swg.Client.Core.Characters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Autofac;
using NUnit.Framework;
using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Assets.Tre;
using Swg.Client.Core.Characters.Skills;
using Swg.Client.Core.Configuration;
using Swg.Client.Core.Networking.Game.Replication;
using Swg.Client.Core.World;
using Swg.Client.Core;
using Swg.Client.Runtime;
using Swg.Client.Runtime.Assets.Tre;
using Swg.Client.Runtime.Localization;
using Swg.Client.Runtime.Networking;
using Swg.Client.Runtime.Commands;
using Swg.Client.Runtime.Assets.UI;
using Swg.Client.Runtime.UI.Skills;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Swg.Client.Tests.Editor
{
    public sealed class SkillsChecks
    {
        static string Labels(Transform content) => string.Join("\n", content.GetComponentsInChildren<SkillsDetailRow>().Select(row => row.label.text));
        static byte[] Bytes(Action<BinaryWriter> write)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            write(writer);
            return stream.ToArray();
        }
        static void Ascii(BinaryWriter writer, string text) { writer.Write((ushort)text.Length); writer.Write(System.Text.Encoding.ASCII.GetBytes(text)); }

        [Test]
        public void SkillListBaselineAndNameBasedDeltasPreserveLearnedSkills()
        {
            var reader = new ByteReader(Bytes(writer =>
            {
                writer.Write(0); writer.Write(0); // Currency.
                writer.Write(0); writer.Write(0); // Base HAM.
                writer.Write(2); writer.Write(1);
                Ascii(writer, "crafting_artisan_novice"); Ascii(writer, "crafting_artisan_survey_01");
            }));
            var creature = new CreatureSceneObject();
            creature.Apply(CreatureView1.ReadBaseline(reader));
            ReplicationReader.RequireEnd(reader);
            var deltaReader = new ByteReader(Bytes(writer =>
            {
                writer.Write(3); writer.Write(2);
                writer.Write((byte)0); Ascii(writer, "crafting_artisan_survey_01");
                writer.Write((byte)1); Ascii(writer, "social_entertainer_novice");
                writer.Write((byte)1); Ascii(writer, "social_entertainer_novice");
            }));
            var update = new CreatureSceneObjectUpdate();
            Assert.That(CreatureView1.ReadDeltaField(3, deltaReader, update), Is.True);
            ReplicationReader.RequireEnd(deltaReader);
            creature.Apply(update);
            Assert.That(creature.Skills, Is.EquivalentTo(new[] { "crafting_artisan_novice", "social_entertainer_novice" }));
            Assert.That(update.Changes, Is.EqualTo(SceneObjectChange.Skills));
            var copy = (CreatureSceneObject)creature.Copy();
            creature.Skills.Clear();
            Assert.That(copy.Skills.Count, Is.EqualTo(2));
        }

        [Test]
        public void ModifierMapConsumesValuesOnRemovalAndKeepsOtherFieldsAligned()
        {
            var creature = new CreatureSceneObject();
            creature.SkillModifiers["surveying"] = 20;
            var reader = new ByteReader(Bytes(writer =>
            {
                writer.Write(2); writer.Write(3);
                writer.Write((byte)1); Ascii(writer, "surveying"); writer.Write(20); writer.Write(0);
                writer.Write((byte)0); Ascii(writer, "general_assembly"); writer.Write(20); writer.Write(5);
                writer.Write(7.3f);
            }));
            var update = new CreatureSceneObjectUpdate();
            CreatureView4.ReadDeltaField(3, reader, update);
            CreatureView4.ReadDeltaField(7, reader, update);
            ReplicationReader.RequireEnd(reader);
            creature.Apply(update);
            Assert.That(creature.SkillModifiers.ContainsKey("surveying"), Is.False);
            Assert.That(creature.SkillModifiers["general_assembly"], Is.EqualTo(25));
            Assert.That(creature.RunSpeed, Is.EqualTo(7.3f));
        }

        [Test]
        public void PlayerExperienceBaselineAndDeltaNotifyWithoutChangingWaypoints()
        {
            var playerRegistry = new SceneObjectRegistry();
            var currentPlayer = new CurrentPlayer(playerRegistry);
            var state = new WorldState(playerRegistry);
            int notifications = 0;
            currentPlayer.ExperienceChanged += () => notifications++;
            var baseline = new ByteReader(Bytes(writer =>
            {
                writer.Write(1); writer.Write(1);
                writer.Write((byte)0); Ascii(writer, "crafting_general"); writer.Write(1000);
                writer.Write(new byte[8 + 8 + 8 + 8 + 8 + 8]); // Waypoints, force, quests and reserved map.
            }));
            PlayerView8.ReadBaseline(baseline)(currentPlayer);
            ReplicationReader.RequireEnd(baseline);
            int waypointNotifications = 0;
            currentPlayer.WaypointsChanged += () => waypointNotifications++;
            var delta = new ByteReader(Bytes(writer =>
            {
                writer.Write((ushort)1); writer.Write((ushort)0);
                writer.Write(2); writer.Write(2);
                writer.Write((byte)1); Ascii(writer, "crafting_general"); writer.Write(1000);
                writer.Write((byte)0); Ascii(writer, "combat_general"); writer.Write(0);
            }));
            PlayerView8.ReadDelta(delta)(currentPlayer);
            Assert.That(currentPlayer.Experience.ContainsKey("crafting_general"), Is.False);
            Assert.That(currentPlayer.Experience["combat_general"], Is.Zero);
            Assert.That(notifications, Is.EqualTo(2));
            Assert.That(waypointNotifications, Is.Zero);
        }

        [Test]
        public void NativeCatalogDefinesProfessionOrderTreesRequirementsAndUnlocks()
        {
            using var loader = new TreAssetLoader(@"D:\SwgEmu\emu\SWGEmu");
            var catalog = SkillCatalog.Load(loader);
            var artisan = catalog.Skills["crafting_artisan"];
            var engineer = catalog.Skills["crafting_artisan_engineering_04"];
            Assert.That(catalog.Professions.Count, Is.GreaterThan(35));
            Assert.That(catalog.Professions.TakeWhile(catalog.IsBasic).Count(), Is.GreaterThan(6));
            Assert.That(catalog.Professions.SkipWhile(catalog.IsBasic).All(item => !catalog.IsBasic(item)), Is.True);
            Assert.That(catalog.Tree(artisan).Skills.Count, Is.EqualTo(18));
            foreach (var profession in catalog.Professions)
            {
                var tree = catalog.Tree(profession);
                Assert.That(catalog.Tree(profession), Is.SameAs(tree), profession.Name);
                Assert.That(tree.Branches.Count, Is.EqualTo(4), profession.Name);
                Assert.That(tree.Branches.All(branch => branch.Length == 4), Is.True, profession.Name);
                Assert.That(tree.Branches.SelectMany(branch => branch).Concat(new[] { tree.Novice, tree.Master }).Where(skill => skill != null), Is.EquivalentTo(tree.Skills), profession.Name);
            }
            var artisanTree = catalog.Tree(artisan);
            Assert.That(artisanTree.Novice.Name, Is.EqualTo("crafting_artisan_novice"));
            Assert.That(artisanTree.Master.Name, Is.EqualTo("crafting_artisan_master"));
            Assert.That(artisanTree.Branches[0].Select(skill => skill.Name), Is.EqualTo(Enumerable.Range(1, 4).Select(tier => "crafting_artisan_engineering_" + tier.ToString("00"))));
            Assert.That(artisanTree.Destinations[0].Select(profession => profession.Name), Does.Contain("crafting_weaponsmith"));
            Assert.That(engineer.Points, Is.EqualTo(5));
            Assert.That(engineer.ExperienceCost, Is.EqualTo(6000));
            Assert.That(engineer.ExperienceType, Is.EqualTo("crafting_general"));
            Assert.That(catalog.HasPrerequisites(engineer, new List<string> { "crafting_artisan_novice" }), Is.False);
            Assert.That(catalog.HasPrerequisites(engineer, new List<string> { "crafting_artisan_engineering_03" }), Is.True);
            Assert.That(catalog.PointsAvailable(new[] { "crafting_artisan_novice", "crafting_artisan_novice" }), Is.EqualTo(235));
            Assert.That(catalog.Schematics(engineer), Is.Not.Empty);
            Assert.That(engineer.Modifiers["armor_customization"], Is.EqualTo(20));
            Assert.That(engineer.Label, Does.Contain("Engineering IV"));
            Assert.That(catalog.Skills["combat_commando_heavyweapon_speed_01"].Modifiers["heavy_rocket_launcher_speed"], Is.EqualTo(20));
        }

        [Test]
        public void SkillPrefabUsesNativeCutCornersAndUpwardYellowExperience()
        {
            var view = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Skills/Skills.prefab").GetComponent<SkillsWindow>();
            foreach (var box in new[] { view.master, view.novice }.Concat(view.branches))
            {
                Assert.That(box.button.targetGraphic.color, Is.EqualTo(view.theme.skill), box.button.name);
                Assert.That(box.button.colors.normalColor, Is.EqualTo(Color.white), "Button tint must not multiply the theme color twice.");
                Assert.That(box.progress.fillMethod, Is.EqualTo(Image.FillMethod.Vertical));
                Assert.That(box.progress.fillOrigin, Is.EqualTo((int)Image.OriginVertical.Bottom));
                Assert.That(box.progress.color, Is.EqualTo(view.theme.skillExperience));
                Assert.That(box.progressBackground, Is.Not.Null);
                var binding = box.button.GetComponent<TreTextureBinding>();
                Assert.That(((TrePanel)binding.asset).layers[0].style, Is.EqualTo("Styles.New.tree_default"));
            }
            Assert.That((Color32)view.theme.learnedSkill, Is.EqualTo(new Color32(1, 184, 5, 255)));
            Assert.That(view.pointsBar.rowCount, Is.EqualTo(1));
            Assert.That(view.destinations.All(link => link.label.horizontalAlignment == TMPro.HorizontalAlignmentOptions.Center), Is.True);
            var inset = (RectTransform)view.master.button.transform.parent;
            Assert.That(inset.name, Is.EqualTo("Tree content"));
            Assert.That(inset.offsetMin, Is.EqualTo(Vector2.one * 5));
            Assert.That(inset.offsetMax, Is.EqualTo(-Vector2.one * 5));
            Assert.That((Color32)view.detailRowPrefab.transform.Find("Row divider").GetComponent<Image>().color, Is.EqualTo(new Color32(0, 95, 121, 255)));
            Assert.That(view.pointsBar.backgroundColor.a, Is.Zero, "The unfilled rail remains visible underneath the green blocks.");
            Assert.That(view.pointsBar.transform.parent.name, Is.EqualTo("Skill points capsule"));
            using var loader = new TreAssetLoader(@"D:\SwgEmu\emu\SWGEmu");
            var frame = AssetDatabase.LoadAssetAtPath<TrePanel>("Assets/SwgAssets/UI/Skills/SkillBox.asset").Load(loader);
            Assert.That(frame.Pixels[(frame.Width - 1) * 4 + 3], Is.Zero, "The top-right corner is cut away.");
            Assert.That(frame.Pixels[(frame.Height - 1) * frame.Width * 4 + 3], Is.Zero, "The bottom-left corner is cut away.");
            Assert.That(frame.Pixels[((frame.Height / 2) * frame.Width + frame.Width / 2) * 4 + 3], Is.EqualTo(255), "Skill interiors must hide the connectors behind them.");
        }

        [Test]
        public async Task WindowRefreshesProgressOnSkillsAndXpButNotMovement()
        {
            var registry = new SceneObjectRegistry();
            using var scope = ManagedObjectTestScope.Use(registry, builder =>
            {
                builder.RegisterInstance(ClientSettings.From(Path.Combine(Directory.GetParent(Application.dataPath).FullName, "client.ini")));
                builder.RegisterType<TreRuntimeAssets>().SingleInstance();
                builder.RegisterType<StringTableLocalizer>().SingleInstance();
                builder.RegisterType<ClientSkills>().SingleInstance();
                builder.RegisterInstance(new Action<string, string, SwgId>((name, arguments, target) => { }));
                builder.RegisterType<CommandCatalog>().SingleInstance();
                builder.RegisterType<CommandVisuals>().SingleInstance();
            });
            var currentPlayer = GameServices.Resolve<CurrentPlayer>();
            var state = GameServices.Resolve<WorldState>();
            currentPlayer.BeginScene(1);
            state.BeginScene(Vector3.zero, "terrain/corellia.trn");
            var player = new CreatureSceneObject { SwgId = 1 };
            player.Skills.Add("crafting_artisan_novice");
            registry.Add(player);
            var service = GameServices.Resolve<ClientSkills>();
            await service.Loading;
            await GameServices.Resolve<CommandVisuals>().Loading;
            var root = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Skills/Skills.prefab"));
            var window = root.GetComponent<SkillsWindow>();
            bool subscribed = false;
            try
            {
                Invoke(window, "Awake");
                root.SetActive(true);
                Invoke(window, "OnEnable");
                window.StopAllCoroutines(); // This EditMode test drives readiness explicitly.
                subscribed = true;
                typeof(SkillsWindow).GetProperty("Ready").SetValue(window, true);
                window.SelectTab(false);
                Invoke(window, "LateUpdate");
                Assert.That(window.professionList.GetComponentsInChildren<Button>().Count(), Is.EqualTo(1));
                var engineering = service.Catalog.Skills["crafting_artisan_engineering_01"];
                var box = window.branches.Single(item => item.skill == engineering);
                Assert.That(box.progress.gameObject.activeSelf, Is.True);
                currentPlayer.UpdateExperience(values => values[engineering.ExperienceType] = engineering.ExperienceCost / 2);
                Invoke(window, "LateUpdate");
                Assert.That(box.progress.fillAmount, Is.EqualTo(.5f).Within(.02));
                window.novice.button.GetComponent<SkillBoxHover>().OnPointerEnter(null);
                Invoke(window, "LateUpdate");
                Assert.That(window.surrenderButton.gameObject.activeSelf, Is.True);
                Assert.That(window.skillPointRequirement.text, Does.Contain("recover " + window.novice.skill.Points));
                window.points.text = "unchanged";
                registry.NotifyChanged(player, SceneObjectChange.Transform);
                Invoke(window, "LateUpdate");
                Assert.That(window.points.text, Is.EqualTo("unchanged"));
                player.Skills.Add(engineering.Name);
                registry.NotifyChanged(player, SceneObjectChange.Skills);
                Invoke(window, "LateUpdate");
                Assert.That(box.progress.gameObject.activeSelf, Is.False);
                Assert.That(box.progressBackground.gameObject.activeSelf, Is.False);
                Assert.That(box.button.targetGraphic.color, Is.EqualTo(window.theme.learnedSkill));
                window.SelectSkill(service.Catalog.Skills["crafting_artisan_engineering_04"]);
                var unlocks = await service.Unlocks(service.Catalog.Skills["crafting_artisan_engineering_04"]);
                Assert.That(unlocks.Select(item => item.Label), Does.Contain("Bone Armor Chest Plate"));
                Assert.That(unlocks.All(label => !string.IsNullOrWhiteSpace(label.Label)), Is.True);
                Assert.That(string.Join(" ", window.grantedModifiers.GetComponentsInChildren<SkillsDetailRow>().Select(row => row.value.text)), Does.Contain("+20"));
                Invoke(window, "OnDisable");
                subscribed = false;
                UnityEngine.Object.DestroyImmediate(root);
                root = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Skills/Skills.prefab"));
                window = root.GetComponent<SkillsWindow>();
                Invoke(window, "Awake");
                root.SetActive(true);
                Invoke(window, "OnEnable");
                window.StopAllCoroutines(); // This EditMode test drives readiness explicitly.
                subscribed = true;
                typeof(SkillsWindow).GetProperty("Ready").SetValue(window, true);
                window.SelectTab(true);
                Invoke(window, "LateUpdate");
                Assert.That(window.characterLists.activeSelf, Is.False);
                Assert.That(window.professionList.GetComponentsInChildren<Button>().Length, Is.EqualTo(service.Catalog.Professions.Count));
            }
            finally
            {
                if (subscribed) Invoke(window, "OnDisable");
                UnityEngine.Object.DestroyImmediate(root);
            }
        }
        [Test]
        public async Task ParameterizedGrantsKeepTheirArgumentsAndSurrenderWaitsForReplication()
        {
            var registry = new SceneObjectRegistry();
            var sent = new List<(string Name, string Arguments, SwgId Target)>();
            using var scope = ManagedObjectTestScope.Use(registry, builder =>
            {
                builder.RegisterInstance(ClientSettings.From(Path.Combine(Directory.GetParent(Application.dataPath).FullName, "client.ini")));
                builder.RegisterType<TreRuntimeAssets>().SingleInstance();
                builder.RegisterType<StringTableLocalizer>().SingleInstance();
                builder.RegisterType<ClientSkills>().SingleInstance();
                builder.RegisterInstance(new Action<string, string, SwgId>((name, arguments, target) => sent.Add((name, arguments, target))));
                builder.RegisterType<CommandCatalog>().SingleInstance();
            });
            var currentPlayer = GameServices.Resolve<CurrentPlayer>();
            var state = GameServices.Resolve<WorldState>();
            currentPlayer.BeginScene(1);
            state.BeginScene(Vector3.zero, "terrain/corellia.trn");
            var player = new CreatureSceneObject { SwgId = 1 };
            player.Skills.Add("crafting_artisan_survey_04");
            registry.Add(player);
            var service = GameServices.Resolve<ClientSkills>();
            await service.Loading;
            foreach (string ability in new[] { "startMusic+ballad", "startDance+basic2", "flourish+1", "language+rodese" })
            {
                var skill = service.Catalog.Skills.Values.First(value => value.Commands.Contains(ability));
                var grants = await service.Unlocks(skill);
                var grant = grants.Single(value => value.Command?.Key.Equals(ability, StringComparison.OrdinalIgnoreCase) == true);
                string argument = ability.Substring(ability.IndexOf('+') + 1);
                Assert.That(grant.Label, Does.Contain("(" + argument + ")"));
                Assert.That(grant.Command.Arguments, Is.EqualTo(argument));
                Assert.That(grant.Description, Does.Contain(" " + argument));
            }
            var survey = service.Catalog.Skills["crafting_artisan_survey_04"];
            int before = service.AvailablePoints;
            Assert.That(service.Surrender(service.Catalog.Skills["crafting_artisan_master"]), Is.False);
            Assert.That(sent, Is.Empty);
            Assert.That(service.Surrender(survey), Is.True);
            Assert.That(sent.Single(), Is.EqualTo(("surrenderSkill", survey.Name, currentPlayer.SwgId)));
            Assert.That(service.Learned(survey), Is.True, "Only replicated updates remove skills.");
            Assert.That(service.AvailablePoints, Is.EqualTo(before));
            player.Skills.Remove(survey.Name);
            registry.NotifyChanged(player, SceneObjectChange.Skills);
            Assert.That(service.AvailablePoints, Is.EqualTo(before + survey.Points));
        }

        static void Invoke(object target, string method) => target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
    }
}
