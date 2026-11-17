using System;
using System.Reflection;
using NUnit.Framework;
using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;
using Swg.Client.Core;
using Swg.Client.Core.Characters;
using Swg.Client.Core.Networking.Game.Replication;
using Swg.Client.Core.World;
using Swg.Client.Core.World.SceneObjects;
using Swg.Client.Core.Assets.Appearances;
using Swg.Client.Runtime;
using Swg.Client.Runtime.Localization;
using Swg.Client.Runtime.UI.Targeting;
using Swg.Client.Runtime.World.SceneObjects;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Swg.Client.Tests.Editor
{
    public sealed class OverheadNameplateChecks
    {
        [TestCase(0, -15)]
        [TestCase(20, 5)]
        [TestCase(-100, -50)]
        [TestCase(100, 50)]
        public void StandingAttackModifierUsesNativeBaseChanceAndNeutralReference(int rangeAccuracy, int expected)
        {
            var ranges = new WeaponRanges(50, 2, 4, rangeAccuracy, rangeAccuracy, rangeAccuracy);
            Assert.That(ranges.TryAttackModifierAt(3, out int modifier), Is.True);
            Assert.That(modifier, Is.EqualTo(expected));
            Assert.That(ranges.TryAttackModifierAt(5, out _), Is.False);
            Assert.That(default(WeaponRanges).TryAttackModifierAt(0, out _), Is.False);
        }

        [TestCase(0, -10)]
        [TestCase(5, 5)]
        [TestCase(10, 20)]
        [TestCase(15, 0)]
        [TestCase(20, -20)]
        public void AccuracyInterpolatesTheServerWeaponCurve(float distance, int expected)
        {
            var ranges = new WeaponRanges(10, 10, 20, -10, 20, -20);
            Assert.That(ranges.TryAccuracyAt(distance, out int actual), Is.True);
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void UnknownAndOutOfRangeWeaponsDoNotInventAnAccuracyValue()
        {
            Assert.That(default(WeaponRanges).TryAccuracyAt(0, out _), Is.False);
            Assert.That(new WeaponRanges(1, 10, 20, 0, 15, -15).TryAccuracyAt(20.01f, out _), Is.False);
            Assert.That(new WeaponRanges(1, 0, 20, -10, 20, -20).TryAccuracyAt(0, out int pointBlank), Is.True);
            Assert.That(pointBlank, Is.EqualTo(-10));
            var melee = new WeaponRanges(1, 5, 5, 0, 3, 5);
            Assert.That(melee.TryAccuracyAt(5, out int maximum), Is.True);
            Assert.That(maximum, Is.EqualTo(5));
        }

        [Test]
        public void PrefabSharesTheHamAndPostureWidgetForHoverAndSelection()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Targeting/TargetHud.prefab");
            var hud = prefab.GetComponent<TargetHud>();
            Assert.That(hud.selectedNameplate, Is.Not.Null);
            Assert.That(hud.hoverNameplate, Is.Not.Null);
            Assert.That(hud.hoverNameplate, Is.Not.SameAs(hud.selectedNameplate));
            Assert.That(prefab.GetComponentsInChildren<OverheadNameplate>(true).Length, Is.EqualTo(2), "No orphan nameplates may draw default bars.");
            var plate = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Targeting/OverheadNameplate.prefab").GetComponent<OverheadNameplate>();
            Assert.That(plate.objectName.raycastTarget, Is.False);
            Assert.That(plate.attackModifier.raycastTarget, Is.False);
            Assert.That(PrefabUtility.GetCorrespondingObjectFromSource(plate.status).gameObject.name, Is.EqualTo("CreatureStatus"));
        }

        [Test]
        public void NameplateUsesLiveHamAndRangeAndShowsOnlyTheNameForAnUnattackableNpc()
        {
            var registry = new SceneObjectRegistry();
            using var scope = ManagedObjectTestScope.Use(registry);
            var state = GameServices.Resolve<WorldState>();
            var player = GameServices.Resolve<CurrentPlayer>();
            player.BeginScene(1);
            state.BeginScene(Vector3.zero, "terrain/test.trn");
            var actor = new CreatureSceneObject { SwgId = 1 };
            registry.Add(actor);
            var target = new CreatureSceneObject { SwgId = 2, Name = "Miner", PvpStatus = 1,
                Ham = new HamPool { Health = new HamElement(100, 100, 0), Action = new HamElement(80, 80, 0), Mind = new HamElement(60, 60, 0) } };
            registry.Add(target);
            player.WeaponRanges = new WeaponRanges(50, 10, 20, 0, 0, 0);
            var scene = EditorSceneManager.NewPreviewScene();
            var owner = new GameObject("Nameplate check");
            SceneManager.MoveGameObjectToScene(owner, scene);
            try
            {
                GameObject Body(SwgId id, Vector3 position)
                {
                    var body = new GameObject("Body"); body.transform.SetParent(owner.transform); body.transform.position = position;
                    body.AddComponent<ManagedSceneObject>().Bind(id);
                    body.AddComponent<TargetableSceneObject>().Initialize(id, new AppearanceData { BoundsMin = new Vector3(-.3f, 0, -.3f), BoundsMax = new Vector3(.3f, 2, .3f) }, true);
                    return body;
                }
                Body(1, Vector3.zero);
                var body = Body(2, Vector3.forward * 10);
                var cameraRoot = new GameObject("Camera"); cameraRoot.transform.SetParent(owner.transform);
                var camera = cameraRoot.AddComponent<Camera>(); camera.enabled = false; camera.pixelRect = new Rect(0, 0, 800, 600);
                var canvas = new GameObject("Overlay", typeof(RectTransform), typeof(Canvas)); canvas.transform.SetParent(owner.transform);
                var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Targeting/OverheadNameplate.prefab"), canvas.transform);
                var plate = root.GetComponent<OverheadNameplate>();
                void Set(string name, object value) => typeof(OverheadNameplate).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(plate, value);
                Set("state", state); Set("player", player); Set("names", new SceneObjectNameResolver(null, new StringTableLocalizer(null)));
                plate.Show(2, camera);
                Assert.That(root.activeSelf, Is.True);
                Assert.That(plate.objectName.text, Is.EqualTo("Miner"));
                Assert.That(plate.objectName.color, Is.EqualTo(plate.neutralColor));
                Assert.That(plate.attackModifier.text, Is.EqualTo("-15"));
                Assert.That(plate.status.gameObject.activeSelf, Is.True);
                Assert.That(plate.status.ham.currentBars[1].transform.parent.GetComponent<RectTransform>().anchorMax.x, Is.EqualTo(.8f));
                target.Ham.Health = new HamElement(50, 100, 0);
                plate.Show(2, camera);
                Assert.That(plate.status.ham.currentBars[0].fillAmount, Is.EqualTo(.5f));
                body.transform.position = Vector3.forward * 21;
                plate.Show(2, camera);
                Assert.That(plate.attackModifier.text, Is.EqualTo("--"));
                Assert.That(plate.attackModifier.color, Is.EqualTo(plate.outOfRangeColor));
                target.PvpStatus = 3;
                plate.Show(2, camera);
                Assert.That(plate.objectName.color, Is.EqualTo(plate.hostileColor));
                target.PvpStatus = 0;
                plate.Show(2, camera);
                Assert.That(plate.objectName.color, Is.EqualTo(Color.white));
                Assert.That(plate.status.gameObject.activeSelf, Is.False);
                Assert.That(plate.attackModifier.gameObject.activeSelf, Is.False);
                var group = new GroupSceneObject { SwgId = 100 };
                group.Apply(new GroupSceneObjectUpdate { Members = list => { list.Add(new GroupMember(3, "Leader")); list.Add(new GroupMember(2, "Miner")); } });
                registry.Add(group); actor.GroupSwgId = 100;
                Assert.That(plate.ColorFor(target), Is.EqualTo(plate.memberColor));
                group.Apply(new GroupSceneObjectUpdate { Members = list => { list.Clear(); list.Add(new GroupMember(2, "Miner")); } });
                Assert.That(plate.ColorFor(target), Is.EqualTo(plate.leaderColor));
                plate.Show(1, camera);
                Assert.That(root.activeSelf, Is.False, "Never display the current player's own nameplate.");
                plate.Show(default, camera);
                Assert.That(root.activeSelf, Is.False);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
