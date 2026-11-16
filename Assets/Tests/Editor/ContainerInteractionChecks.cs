using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Swg.Client.Core;
using Swg.Client.Core.Characters;
using Swg.Client.Core.Configuration;
using Swg.Client.Core.World;
using Swg.Client.Core.World.SceneObjects;
using Swg.Client.Core.Networking.Game.Packets.Scene;
using Swg.Client.Core.Networking.Game.Packets.Ui;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Runtime.Assets.Tre;
using Swg.Client.Runtime.Networking;
using Swg.Client.Runtime.UI;
using Swg.Client.Runtime.UI.Hud;
using Swg.Client.Runtime.UI.Inventory;
using Swg.Client.Runtime.UI.Windows;
using Swg.Client.Runtime.World.SceneObjects;
using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;
namespace Swg.Client.Tests.Editor
{
    public sealed class ContainerInteractionChecks
    {
        [TestCase(0x2005)] [TestCase(0x2015)] [TestCase(0x2027)] [TestCase(0x2034)] public void ContainerTypesOfferOpenAndTheBlueUseCursor(int type)
        {
            Assert.That(ObjectMenuDefaults.TryGetDefaultAction(new TangibleSceneObject(), (ObjectTypeId)type, out var action), Is.True);
            Assert.That(action, Is.SameAs(ObjectMenuAction.Open));
            Assert.That(ClientCursor.ForObject(new TangibleSceneObject(), (ObjectTypeId)type, true), Is.EqualTo(ClientCursor.WorldCursor.Use));
        }
        [TestCase(0x200a)] [TestCase(0x2018)] public void FoodAndDrinkKeepServerUseAndUseTheEatCursor(int type)
        {
            Assert.That(ObjectMenuDefaults.TryGetDefaultAction(new TangibleSceneObject(), (ObjectTypeId)type, out var action), Is.True);
            Assert.That(action, Is.SameAs(ObjectMenuAction.Use));
            Assert.That(ClientCursor.ForObject(new TangibleSceneObject(), (ObjectTypeId)type, true), Is.EqualTo(ClientCursor.WorldCursor.Eat));
        }
        [Test] public async Task ContainerRepliesOpenIndependentViewsWithoutReplacingThePlayerInventory()
        {
            using var assets = new TreRuntimeAssets(ClientSettings.From(Path.Combine(Application.dataPath,"../client.ini")));
            var state = GameServicesForTests.CreateState(out var player);
            player.BeginScene(1);
            state.Templates = new ObjectTemplateCatalog(assets.Loader);
            state.SceneObjects.Add(new CreatureSceneObject
            {
                SwgId = 1
            }
            );
            state.SceneObjects.Add(new TangibleSceneObject
            {
                SwgId = 2, ParentSwgId = 1, TemplatePath = "object/tangible/inventory/shared_character_inventory.iff"
            }
            );
            state.SceneObjects.Add(new TangibleSceneObject
            {
                SwgId = 3, TemplatePath = "object/tangible/furniture/cheap/shared_chest_s01.iff"
            }
            );
            state.SceneObjects.Add(new TangibleSceneObject
            {
                SwgId = 4, ParentSwgId = 3, Volume = 2
            }
            );
            using var inventory = new ClientInventory(state, _ =>
            {
            }
            , player);
            SwgId opened = default;
            inventory.ContainerOpened += id => opened = id;
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            writer.Write((ushort)4);
            writer.Write(ClientOpenContainerPacket.Opcode);
            writer.Write(3UL);
            writer.Write(0u);
            writer.Write((ushort)0);
            inventory.Receive(ClientOpenContainerPacket.ReadFrom(new ByteReader(stream.ToArray())));
            await Task.Delay(30);
            Assert.That(opened, Is.EqualTo(new SwgId(3)));
            Assert.That(inventory.ContainerSwgId, Is.EqualTo(new SwgId(2)));
            Assert.That(inventory.Contents(3).Single().SwgId, Is.EqualTo(new SwgId(4)));
            Assert.That(inventory.Used(3), Is.EqualTo(2));
            state.SceneObjects.Reparent(4,2);
            Assert.That(inventory.Contents(3), Is.Empty);
            Assert.That(inventory.Items.Single().SwgId, Is.EqualTo(new SwgId(4)));
        }
        [Test] public void DefaultFurnitureExaminesAndContainerOpeningUsesTheSharedCommandPath()
        {
            using var assets = new TreRuntimeAssets(ClientSettings.From(Path.Combine(Application.dataPath,"../client.ini")));
            var state = GameServicesForTests.CreateState(out var player);
            state.Templates = new ObjectTemplateCatalog(assets.Loader);
            state.SceneObjects.Add(new TangibleSceneObject
            {
                SwgId = 3
            }
            );
            string command = null;
            SwgId target = default;
            using var interactions = new ClientObjectInteractions(state,_=>
            {
            }
            ,_=>
            {
            }
            ,_=>Assert.Fail("Open is a native client command"),(name,args,id)=>
            {
                command=name;target=id;
            }
            ,player);
            interactions.ExamineRequested += id => target=id;
            interactions.ActivateDefault(3);
            Assert.That(target, Is.EqualTo(new SwgId(3)));
            interactions.SelectMenuAction(3, ObjectMenuAction.Open);
            Assert.That(command, Is.EqualTo("openContainer"));
            Assert.That(target, Is.EqualTo(new SwgId(3)));
        }
        [Test] public void ContainerIsAnInventoryPrefabVariantWithSharedCellsAndCapacity()
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Inventory/Container.prefab");
            var view=prefab.GetComponent<InventoryWindow>();
            Assert.That(PrefabUtility.GetPrefabAssetType(prefab), Is.EqualTo(PrefabAssetType.Variant));
            Assert.That(view.itemPrefab, Is.SameAs(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Inventory/Inventory.prefab").GetComponent<InventoryWindow>().itemPrefab));
            Assert.That(view.characterPanel.gameObject.activeSelf, Is.False);
            Assert.That(view.capacityBar, Is.Not.Null);
            Assert.That(view.examineToggle.isOn, Is.True);
        }
        [Test] public void BuffWindowDragsAcrossTheScrollSurfaceAndKeepsNativeConsumableIcons()
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Hud/BuffHud.prefab");
            var hud=prefab.GetComponent<BuffHud>();
            Assert.That(hud.panel.GetComponent<WindowHandle>().edges, Is.EqualTo(WindowEdges.None));
            Assert.That(hud.content.parent.GetComponent<WindowScrollRect>().window, Is.SameAs(hud.panel.GetComponent<UiWindow>()));
            Assert.That(hud.panel.transform.Find("Drag handle"), Is.Null);
            Assert.That(hud.definitions.Single(d=>d.name=="food.drink_vasarian_brandy").icon, Is.Not.Null);
            var root = new GameObject("Drag check",typeof(RectTransform));
            var events = new GameObject("Events",typeof(EventSystem));
            try
            {
                ((RectTransform)root.transform).sizeDelta=new Vector2(1000,800);
                var child=new GameObject("Window",typeof(RectTransform),typeof(UiWindow));
                child.transform.SetParent(root.transform,false);
                var rect=(RectTransform)child.transform;
                rect.sizeDelta=new Vector2(100,100);
                var surface=child.AddComponent<WindowScrollRect>();
                surface.window=child.GetComponent<UiWindow>();
                var pointer=new PointerEventData(events.GetComponent<EventSystem>())
                {
                    position=new Vector2(100,100),button=PointerEventData.InputButton.Left
                }
                ;
                surface.OnInitializePotentialDrag(pointer);
                var before=rect.position;
                pointer.position+=new Vector2(30,20);
                surface.OnDrag(pointer);
                Assert.That((rect.position-before- new Vector3(30,20,0)).magnitude, Is.LessThan(.01f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(events);
            }
        }
    }
}
