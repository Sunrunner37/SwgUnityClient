using Swg.Client.Core.Characters;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Autofac;
using NUnit.Framework;
using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;
using Swg.Client.Core.Configuration;
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
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace Swg.Client.Tests.Editor
{
    public sealed class SkillsWindowPlayChecks
    {
        [UnityTest]
        public IEnumerator ReopeningResumesTheSelectedSkillsPendingOrCompletedUnlocks()
        {
            yield return new EnterPlayMode();
            // Isolate the widget from frontend navigation; keep the normal global services alive.
            var initialScene = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Skills window lifecycle test"));
            yield return SceneManager.UnloadSceneAsync(initialScene);
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
            yield return Until(() => service.Loading.IsCompleted);
            Assert.That(service.Loading.Exception, Is.Null);
            var skill = service.Catalog.Skills["crafting_artisan_engineering_04"];
            var unlocks = (Dictionary<string, Task<ClientSkills.Unlock[]>>)typeof(ClientSkills).GetField("unlocks", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(service);
            var root = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Skills/Skills.prefab"));
            var window = root.GetComponent<SkillsWindow>();
            try
            {
                root.SetActive(true);
                yield return Until(() => window.Ready);
                for (int pass = 0; pass < 2; pass++)
                {
                    var delayed = new TaskCompletionSource<ClientSkills.Unlock[]>();
                    unlocks[skill.Name] = delayed.Task;
                    window.SelectSkill(skill);
                    Assert.That(Labels(window.grantedUnlocks), Is.Empty);
                    root.SetActive(false);
                    if (pass == 0) delayed.SetResult(new[] { new ClientSkills.Unlock("Completed while closed") });
                    root.SetActive(true);
                    yield return Until(() => window.Ready);
                    Assert.That(window.selectedTitle.text, Is.EqualTo(skill.Label));
                    if (pass == 1)
                    {
                        Assert.That(Labels(window.grantedUnlocks), Is.Empty);
                        delayed.SetResult(new[] { new ClientSkills.Unlock("Completed after reopening") });
                    }
                    yield return Until(() => Labels(window.grantedUnlocks).Contains("Completed"));
                    Assert.That(Labels(window.grantedUnlocks), Is.EqualTo(delayed.Task.Result[0].Label));
                    Assert.That(service.Unlocks(skill), Is.SameAs(delayed.Task), "Reopening should observe the existing cached load.");
                }
            }
            finally
            {
                root.SetActive(false);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }
        [UnityTest]
        public IEnumerator OfflineSceneExposesEagerSkillsToTheWindow()
        {
            yield return new EnterPlayMode();
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/UITesting.unity", new LoadSceneParameters(LoadSceneMode.Single));
            var service = GameServices.Resolve<ClientSkills>();
            yield return Until(() => service.Loading.IsCompleted);
            Assert.That(service.Loading.Exception, Is.Null);
            yield return Until(() => service.Player?.Skills.Count > 0);
            var window = UnityEngine.Object.FindAnyObjectByType<SkillsWindow>(FindObjectsInactive.Include);
            window.ToggleOpen();
            yield return Until(() => window.Ready);
            Assert.That(window.professionTitle.text, Is.EqualTo("ARTISAN"));
            Assert.That(window.novice.button.targetGraphic.color, Is.EqualTo(window.theme.learnedSkill));
            yield return null;
            foreach (var tab in new[] { window.characterTab, window.allTab })
            {
                var label = tab.GetComponentInChildren<TMP_Text>();
                label.ForceMeshUpdate();
                Assert.That(label.isTextOverflowing, Is.False, tab.name);
            }
            var currentPlayer = GameServices.Resolve<CurrentPlayer>();
            var state = GameServices.Resolve<WorldState>();
            var artisan = service.Catalog.Skills["crafting_artisan"];
            window.SelectProfession(artisan);
            yield return null;
            var rowButton = window.professionList.GetComponentsInChildren<Button>().Single(button => button.name == artisan.Name);
            Assert.That(rowButton.colors.normalColor, Is.EqualTo(window.theme.accent));
            var first = service.Catalog.Skills["crafting_artisan_engineering_01"];
            currentPlayer.UpdateExperience(values => values[first.ExperienceType] = first.ExperienceCost / 2);
            window.SelectSkill(first);
            yield return null;
            Assert.That(window.experienceBar.gameObject.activeSelf, Is.True);
            Assert.That(window.experienceBar.Fill, Is.EqualTo(.5f).Within(.01f));
            Assert.That(window.experienceBar.color, Is.EqualTo(window.theme.skillExperience));
            currentPlayer.UpdateExperience(values => values[first.ExperienceType] = first.ExperienceCost);
            yield return null;
            var box = window.branches.Single(item => item.skill == first);
            Assert.That(box.progress.color, Is.EqualTo(window.theme.capacityFill));
            Assert.That(window.experienceBar.color, Is.EqualTo(window.theme.capacityFill));
            Assert.That(rowButton.colors.normalColor, Is.EqualTo(window.theme.accent), "Selecting a skill must not clear the profession highlight.");
            window.SelectSkill(service.Catalog.Skills["crafting_artisan_engineering_04"]);
            yield return null;
            Assert.That(window.experienceBar.gameObject.activeSelf, Is.False, "A skill missing its prerequisites has no active XP bar.");
            Assert.That(window.skillPointRequirement.rectTransform.position.y, Is.GreaterThan(window.points.rectTransform.position.y));

            Canvas.ForceUpdateCanvases();
            var scroll = window.modifiers.GetComponentInParent<ScrollRect>();
            scroll.verticalNormalizedPosition = 1;
            var pointer = new PointerEventData(EventSystem.current) { scrollDelta = new Vector2(0, -3) };
            Assert.That(scroll.viewport.GetComponent<Image>().raycastTarget, Is.True);
            ExecuteEvents.ExecuteHierarchy(scroll.viewport.gameObject, pointer, ExecuteEvents.scrollHandler);
            Assert.That(scroll.verticalNormalizedPosition, Is.LessThan(.999f), "Wheel events over the list should reach its native ScrollRect.");

            var links = window.destinations[0];
            links.label.ForceMeshUpdate();
            var link = Enumerable.Range(0, links.label.textInfo.linkCount).Select(index => links.label.textInfo.linkInfo[index]).Single(info => info.GetLinkID() == "crafting_weaponsmith");
            var character = links.label.textInfo.characterInfo[link.linkTextfirstCharacterIndex];
            var canvas = window.GetComponentInParent<Canvas>().rootCanvas;
            pointer.position = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, links.label.transform.TransformPoint((character.bottomLeft + character.topRight) * .5f));
            links.OnPointerMove(pointer);
            Assert.That(links.label.text, Does.Contain("<u>"));
            links.OnPointerClick(pointer);
            yield return null;
            Assert.That(window.professionTitle.text, Is.EqualTo("WEAPONSMITH"));
            Assert.That(window.allTab.isOn, Is.True);

            var abilitySkill = service.Catalog.Skills.Values.First(skill => skill.Commands.Any(command => command.Equals("applydisease", StringComparison.OrdinalIgnoreCase)));
            window.SelectProfession(service.Catalog.Profession(abilitySkill));
            window.SelectSkill(abilitySkill);
            var unlocks = service.Unlocks(abilitySkill);
            yield return Until(() => unlocks.IsCompleted);
            Assert.That(unlocks.Exception, Is.Null);
            yield return Until(() => window.grantedUnlocks.GetComponentsInChildren<SkillsDetailRow>().Any(row => row.label.text.IndexOf("Apply Disease", StringComparison.OrdinalIgnoreCase) >= 0));
            var abilityRow = window.grantedUnlocks.GetComponentsInChildren<SkillsDetailRow>().First(row => row.label.text.IndexOf("Apply Disease", StringComparison.OrdinalIgnoreCase) >= 0);
            Assert.That(abilityRow.icon.sprite, Is.Not.Null);
            abilityRow.OnPointerEnter(pointer);
            Assert.That(window.tooltip.gameObject.activeSelf, Is.True);
            Assert.That(window.tooltip.label.text.ToLowerInvariant(), Does.Contain("/applydisease"));
            Assert.That(window.tooltip.label.text, Does.Contain("proper item"));
            abilityRow.OnPointerExit(pointer);
            Assert.That(window.tooltip.gameObject.activeSelf, Is.False);
            window.Close();
        }
        static string Labels(Transform content) => string.Join("\n", content.GetComponentsInChildren<SkillsDetailRow>().Select(row => row.label.text));
        static IEnumerator Until(Func<bool> ready)
        {
            float deadline = Time.realtimeSinceStartup + 30;
            while (!ready())
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), "Skills window did not finish its lifecycle transition.");
                yield return null;
            }
        }
        [UnityTearDown]
        public IEnumerator ExitPlayModeAfterCheck()
        {
            if (Application.isPlaying) yield return new ExitPlayMode();
        }
    }
}
