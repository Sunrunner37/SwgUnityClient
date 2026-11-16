using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Swg.Client.Core;
using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;
using Swg.Client.Core.World;
using Swg.Client.Core.World.SceneObjects;
using Swg.Client.Runtime.Networking;
using Swg.Client.Runtime.UI.Inventory;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Swg.Client.Tests.Editor
{
    public sealed class InventoryDestroyChecks
    {
        [Test]
        public void InventoryDestroyRequiresConfirmationAndLeavesRemovalToTheServer()
        {
            var state = GameServicesForTests.CreateState(out var player);
            player.BeginScene(1);
            state.SceneObjects.Add(new CreatureSceneObject { SwgId = 1 });
            state.SceneObjects.Add(new TangibleSceneObject { SwgId = 2, ParentSwgId = 1, TemplatePath = "object/tangible/inventory/shared_character_inventory.iff" });
            state.SceneObjects.Add(new TangibleSceneObject { SwgId = 3, ParentSwgId = 2 });
            var sent = new List<(string Command, SwgId Target)>();
            using var interactions = new ClientObjectInteractions(state, _ => { }, _ => { }, _ => Assert.Fail("Destroy is client-confirmed"), (command, args, target) => sent.Add((command, target)), player, new ObjectMenuDefaults(state, player));
            SwgId requested = default;
            interactions.DestroyRequested += id => requested = id;
            interactions.SelectMenuAction(3, ObjectMenuAction.Destroy);
            Assert.That(requested, Is.EqualTo((SwgId)3));
            Assert.That(sent, Is.Empty);
            interactions.Destroy(3);
            Assert.That(sent.Single(), Is.EqualTo(("serverDestroyObject", (SwgId)3)));
            Assert.That(state.SceneObjects.TryGet(3, out _), Is.True);
            var entry = new ObjectMenuDefaults(state, player).Create(state.SceneObjects.ChildrenOf(2).Single()).Single(item => item.RadialId == 14);
            Assert.That(entry.Callback, Is.EqualTo(1));
        }

        [Test]
        public void NestedInventoryItemsCanBeDestroyedButMovedItemsAndWorldObjectsCannot()
        {
            var state = GameServicesForTests.CreateState(out var player);
            player.BeginScene(1);
            state.SceneObjects.Add(new CreatureSceneObject { SwgId = 1 });
            state.SceneObjects.Add(new TangibleSceneObject { SwgId = 2, ParentSwgId = 1, TemplatePath = "object/tangible/inventory/shared_character_inventory.iff" });
            state.SceneObjects.Add(new TangibleSceneObject { SwgId = 3, ParentSwgId = 2 });
            state.SceneObjects.Add(new TangibleSceneObject { SwgId = 4, ParentSwgId = 3 });
            state.SceneObjects.Add(new TangibleSceneObject { SwgId = 5 });
            int commands = 0;
            using var interactions = new ClientObjectInteractions(state, _ => { }, _ => { }, _ => Assert.Fail(), (_, _, _) => commands++, player, new ObjectMenuDefaults(state, player));
            interactions.Destroy(4);
            Assert.That(commands, Is.EqualTo(1));
            state.SceneObjects.Reparent(3, default);
            interactions.Destroy(4);
            interactions.Destroy(5);
            interactions.Destroy(2);
            Assert.That(commands, Is.EqualTo(1));
        }

        [Test]
        public void DestroyPopupUsesAnInactivePanelAndExplicitNoYesBindings()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Inventory/DestroyItem.prefab");
            var view = prefab.GetComponent<DestroyItemWindow>();
            Assert.That(view.panel.activeSelf, Is.False);
            Assert.That(view.prompt.text, Does.StartWith("Really destroy "));
            Assert.That(view.panel.transform.Find("No").GetComponent<Button>().onClick.GetPersistentMethodName(0), Is.EqualTo(nameof(DestroyItemWindow.Cancel)));
            Assert.That(view.panel.transform.Find("Yes").GetComponent<Button>().onClick.GetPersistentMethodName(0), Is.EqualTo(nameof(DestroyItemWindow.Confirm)));
            Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Hud/WorldHud.prefab").GetComponentInChildren<DestroyItemWindow>(true), Is.Not.Null);
        }
    }
}
