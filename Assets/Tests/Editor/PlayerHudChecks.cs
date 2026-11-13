using System.IO;
using NUnit.Framework;
using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Replication;
using Swg.Client.Runtime.UI.Hud;
using UnityEditor;
using UnityEngine;

namespace Swg.Client.Tests.Editor
{
    public sealed class PlayerHudChecks
    {
        [Test]
        public void WorldHudIncludesAnEditablePlayerPanel()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/WorldHud.prefab");
            var player = prefab.GetComponentInChildren<PlayerHud>(true);
            Assert.That(player, Is.Not.Null);
            Assert.That(player.playerName, Is.Not.Null);
            var panel = (RectTransform)player.panel.transform;
            Assert.That(panel.anchorMin, Is.EqualTo(Vector2.up));
            Assert.That(player.ham.currentBars.Length, Is.EqualTo(3));
        }

        [Test]
        public void PlayerBarsReflectReplicatedHamAndWoundChanges()
        {
            var root = PrefabUtility.LoadPrefabContents("Assets/Prefabs/UI/PlayerHud.prefab");
            try
            {
                var bars = root.GetComponent<PlayerHud>().ham;
                var creature = new CreatureSceneObject();
                new GameObjectUpdate
                {
                    Ham = new[] { 800, 0, 0, 450, 0, 0, 900, 0, 0 },
                    MaxHam = new[] { 1000, 0, 0, 750, 0, 0, 1000, 0, 0 },
                    Wounds = new[] { 100, 0, 0, 0, 0, 0, 0, 0, 0 }
                }.ApplyTo(creature);
                bars.Show(creature.Ham);
                Assert.That(bars.currentBars[0].fillAmount, Is.EqualTo(.8f));
                Assert.That(bars.woundBars[0].fillAmount, Is.EqualTo(.1f));
                Assert.That(bars.capacityBars[0].fillAmount, Is.EqualTo(.9f));
                Assert.That(((RectTransform)bars.currentBars[1].transform.parent).anchorMax.x, Is.EqualTo(.75f));
                Assert.That(bars.woundBars[1].fillAmount, Is.Zero);

                using var bytes = new MemoryStream();
                using var writer = new BinaryWriter(bytes);
                writer.Write(1u); // One vector operation.
                writer.Write(1u); // Server update counter.
                writer.Write((byte)2); // Set an element.
                writer.Write((ushort)0); // Health.
                writer.Write(500);
                var update = new GameObjectUpdate();
                Assert.That(CreatureView6.ReadDeltaField(13, new ByteReader(bytes.ToArray()), update), Is.True);
                update.ApplyTo(creature);
                bars.Show(creature.Ham);
                Assert.That(bars.currentBars[0].fillAmount, Is.EqualTo(.5f));
                Assert.That(bars.woundBars[0].fillAmount, Is.EqualTo(.1f));
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}
