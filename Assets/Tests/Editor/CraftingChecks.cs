using Swg.Client.Core.Characters;
using Swg.Client.Core.Networking.Game.Packets.Scene;
using Swg.Client.Core.Networking.Game.Packets.Data;
using Swg.Client.Core.Networking.Game.Packets.Ui;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Swg.Client.Runtime.Networking;
using Swg.Client.Runtime.Assets.Tre;
using Swg.Client.Runtime.Localization;
using Swg.Client.Core.Configuration;
using Swg.Client.Core.Assets.Formats.ObjectTemplates;
using Swg.Client.Core.Assets.Formats.Ui;
using Swg.Client.Core.Assets.Formats.Dds;
using TMPro;
using System.Text.RegularExpressions;
using Swg.Client.Runtime.UI.Interactions;
using NUnit.Framework;
using Swg.Client.Core;
using Swg.Client.Core.Assets;
using Swg.Client.Core.Crafting;
using Swg.Client.Core.Assets.Tre;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets;
using Swg.Client.Core.Networking.Game.Packets.Crafting;
using Swg.Client.Core.Networking.Game.Replication;
using Swg.Client.Core.World;
using Swg.Client.Core.World.SceneObjects;
using Swg.Client.Runtime.UI.Crafting;
using UnityEditor;
using UnityEngine;
namespace Swg.Client.Tests.Editor
{
    public sealed class CraftingChecks
    {
        static byte[] Bytes(Action<BinaryWriter> write)
        {
            using var stream = new MemoryStream(); using var writer = new BinaryWriter(stream);
            write(writer); return stream.ToArray();
        }
        static void Ascii(BinaryWriter w, string text) { w.Write((ushort)text.Length); w.Write(System.Text.Encoding.ASCII.GetBytes(text)); }
        [TestCase(.24f, 1f, 0f, 0f)]
        [TestCase(.25f, 1f, 1f, 0f)]
        [TestCase(.74f, 1f, 1f, 0f)]
        [TestCase(.75f, 0f, 1f, 0f)]
        public void SlotQualityUsesTheRequestedThresholds(float score, float r, float g, float b)
            => Assert.That(CraftingSlot.QualityColor(score), Is.EqualTo(new Color(r, g, b)));

        [Test]
        public void RemovalMatchesCore3OuterParserThenCallbackWithoutAnExtraReservedField()
        {
            var bytes = Bytes(w => new CraftingRemoveIngredientPacket(1001, 2002, 3, 9).WriteTo(w));
            var reader = new ByteReader(bytes);
            Assert.That(reader.U16(), Is.EqualTo(5));
            Assert.That(reader.U32(), Is.EqualTo(ObjectControllerPacket.Opcode));
            // ObjectControllerMessageCallback::parse stops immediately after the player ID.
            Assert.That(reader.U32(), Is.EqualTo(0x23));
            Assert.That(reader.U32(), Is.EqualTo(0x108));
            Assert.That(reader.U64(), Is.EqualTo(1001));
            // CraftingRemoveIngredientCallback::parse consumes exactly these remaining fields.
            Assert.That(reader.U32(), Is.Zero);
            Assert.That(reader.U32(), Is.EqualTo(3));
            Assert.That(reader.U64(), Is.EqualTo(2002));
            Assert.That(reader.Byte(), Is.EqualTo(9));
            ReplicationReader.RequireEnd(reader);
            Assert.That(bytes.Length, Is.EqualTo(39));
        }

        [Test]
        public void AssemblyUsesFrameRelativeQualityRailsAndASeparateFooter()
        {
            var slot = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Crafting/IngredientSlot.prefab").GetComponent<CraftingSlot>();
            Assert.That(slot.quality.transform.parent.parent, Is.EqualTo(slot.frame.transform));
            Assert.That(slot.quality.transform.parent.GetComponent<RectTransform>().anchorMin, Is.EqualTo(new Vector2(1, 0)));
            Assert.That(slot.quality.transform.parent.GetComponent<RectTransform>().anchorMax, Is.EqualTo(new Vector2(1, 1)));
            var window = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Crafting/Crafting.prefab").GetComponent<CraftingWindow>();
            var footer = (RectTransform)window.assembly.next.transform;
            var body = (RectTransform)window.assembly.root.transform.Find("Body");
            Assert.That(footer.anchorMin, Is.EqualTo(new Vector2(1, 0)));
            Assert.That(footer.pivot, Is.EqualTo(new Vector2(1, 0)));
            Assert.That(footer.anchoredPosition.y + footer.sizeDelta.y, Is.LessThan(body.offsetMin.y));
        }
        [Test]
        public async Task SlotInsertionRemovalAndPrototypeCountdownUseServerState()
        {
            var settings = ClientSettings.From(Path.Combine(Directory.GetParent(Application.dataPath).FullName, "client.ini"));
            using var assets = new TreRuntimeAssets(settings);
            await assets.Ready;
            var registry = new SceneObjectRegistry();
            var currentPlayer = new CurrentPlayer(registry);
            var state = new WorldState(registry) { Templates = new ObjectTemplateCatalog(assets.Loader) };
            currentPlayer.BeginScene(1);
            state.BeginScene(Vector3.zero, "terrain/corellia.trn");
            state.Templates = new ObjectTemplateCatalog(assets.Loader);
            registry.Add(new TangibleSceneObject { SwgId = 2, ParentSwgId = 1, TemplatePath = "object/tangible/inventory/shared_character_inventory.iff" });
            var material = new TangibleSceneObject { SwgId = 9, ParentSwgId = 2, ResourceType = "copper_polysteel" };
            registry.Add(material);
            var schematic = new ManufactureSchematicSceneObject { SwgId = 13 };
            schematic.IngredientQuantities.Add(new List<int>());
            registry.Add(schematic);
            var tool = new TangibleSceneObject { SwgId = 14, Quantity = 0 };
            registry.Add(tool);
            var localizer = new StringTableLocalizer(assets);
            using var commands = new ClientCommands(_ => { }, _ => { }, currentPlayer);
            using var chat = new Swg.Client.Runtime.Networking.Chat.ClientChat(null, state, localizer, new ProseFormatter(), commands, currentPlayer);
            var messages = new List<string>(); chat.ScreenMessageReceived += messages.Add;
            using var inventory = new ClientInventory(state, _ => { }, currentPlayer);
            using var interactions = new ClientObjectInteractions(state, _ => { }, _ => { }, _ => { }, (name, args, target) => { }, currentPlayer, new ObjectMenuDefaults(state, currentPlayer));
            using var catalog = new DraftSchematicCatalog(state, assets, new SceneObjectNameResolver(assets, localizer), localizer, (name, args, target) => { }, currentPlayer);
            var sentAdds = new List<CraftingAddIngredientPacket>();
            var sentRemoves = new List<CraftingRemoveIngredientPacket>();
            var craftingProgress = new CraftingProgress();
            using var session = new ClientCrafting(state, catalog, inventory, interactions, localizer, assets, (name, args, target) => { }, sentAdds.Add, _ => { }, _ => { }, sentRemoves.Add, chat, currentPlayer, craftingProgress);
            await session.Loading;
            void Set(string name, object value) => typeof(ClientCrafting).GetProperty(name).SetValue(session, value);
            Set("CurrentPage", ClientCrafting.Page.Ingredients); Set("SchematicSwgId", (SwgId)13); Set("ToolSwgId", (SwgId)14);
            Set("Slots", new[] { DraftSlotData.ReadFrom(new ByteReader(Bytes(w => {
                Ascii(w, "craft_weapon_ingredients_n"); w.Write(0u); Ascii(w, "cutting_edge"); w.Write((byte)0); w.Write(1u);
                Ascii(w, ""); w.Write(0u); Ascii(w, ""); PacketFieldCodec.Unicode(w, "metal"); w.Write((byte)4); w.Write(30u);
            }))) });
            currentPlayer.UpdateDraftDetails(123, new DraftSchematicDetails { Experiments = new[] { new[] { new DraftSchematicDetails.ExperimentWeight("Shock resistance", 3), new DraftSchematicDetails.ExperimentWeight("Overall quality", 1) } } });
            session.Select(new ClientCrafting.Choice { Entry = new DraftSchematicCatalog.Draft { SchematicCrc = 123 } });
            interactions.Receive(new AttributeListPacket(9, new[] { new AttributeValue("res_shock_resistance", "900"), new AttributeValue("res_quality", "300") }));
            await Task.Yield();
            Assert.That(session.Quality(material), Is.EqualTo(.75f).Within(.001));
            int transfers = 0; session.IngredientOperationStarted += slot => { Assert.That(slot, Is.Zero); transfers++; };
            CraftingResultPacket Reply(uint kind) => CraftingResultPacket.ReadFrom(new ByteReader(Bytes(w => {
                w.Write(ObjectControllerPacket.OperandCount); w.Write(ObjectControllerPacket.Opcode); w.Write(0x1bu); w.Write(0x10cu); w.Write(1ul); w.Write(0u); w.Write(kind); w.Write(0u); w.Write((byte)1);
            })));
            session.Insert(9, 0); Assert.That(sentAdds.Count, Is.EqualTo(1));
            schematic.IngredientQuantities[0].Add(30); session.Receive(Reply(0x107));
            Assert.That(session.SlotMaterial(0), Is.SameAs(material)); Assert.That(session.SlotQuality(0), Is.EqualTo(.75f).Within(.001));
            session.Insert(9, 0); Assert.That(sentAdds.Count, Is.EqualTo(1), "A full slot must not accept another insertion.");
            session.Remove(0); Assert.That(sentRemoves.Single().InventorySwgId, Is.EqualTo((SwgId)2));
            schematic.IngredientQuantities[0].Clear(); session.Receive(Reply(0x108));
            Assert.That(session.SlotMaterial(0), Is.Null); Assert.That(session.SlotQuality(0), Is.Zero); Assert.That(transfers, Is.EqualTo(2));
            Set("CurrentPage", ClientCrafting.Page.Customize); session.Create("Dagger", false, 1);
            var assembled = CraftingAssemblyResultPacket.ReadFrom(new ByteReader(Bytes(w => {
                w.Write(ObjectControllerPacket.OperandCount); w.Write(ObjectControllerPacket.Opcode); w.Write(0x1bu); w.Write(0x1beu); w.Write(1ul); w.Write(0u); w.Write(0u); w.Write(0u); w.Write((byte)1);
            })));
            session.Receive(assembled); Set("CurrentPage", ClientCrafting.Page.Closed);
            tool.Quantity = 42; registry.NotifyChanged(tool, SceneObjectChange.Inventory);
            tool.Quantity = 37; registry.NotifyChanged(tool, SceneObjectChange.Inventory);
            Assert.That(messages.Count, Is.EqualTo(1)); Assert.That(messages[0], Does.Contain("42 seconds"));
        }
        [Test]
        public void ThirdExperimentBubbleSelectsAndDisplaysExactlyThreePoints()
        {
            var parent = new GameObject("Inactive experiment test");
            parent.SetActive(false);
            try
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Crafting/ExperimentRow.prefab");
                var row = UnityEngine.Object.Instantiate(prefab, parent.transform).GetComponent<CraftingExperimentRow>();
                int selected = 0;
                row.Bind(points => selected = points);
                row.bubbles[2].onClick.Invoke();
                row.Show("Damage", .11f, .5f, selected);
                Assert.That(selected, Is.EqualTo(3));
                Assert.That(row.fills.Count(fill => fill.color == (Color)new Color32(255, 225, 0, 255)), Is.EqualTo(3));
                row.Show("Damage", .3f, .5f, 0);
                Assert.That(row.fills.Count(fill => fill.color == (Color)new Color32(255, 225, 0, 255)), Is.Zero);
            }
            finally { UnityEngine.Object.DestroyImmediate(parent); }
        }
        [Test]
        public void ResourceEligibilityUsesTheNativeIndentedAncestry()
        {
            using var loader = new TreAssetLoader(@"D:\SwgEmu\emu\SWGEmu");
            var catalog = ResourceCraftingCatalog.Load(loader);
            Assert.That(catalog.Matches("steel_ditanium", "steel"), Is.True);
            Assert.That(catalog.Matches("steel_ditanium", "metal_ferrous"), Is.True);
            Assert.That(catalog.Matches("iron_colat", "metal_ferrous"), Is.True);
            Assert.That(catalog.Matches("steel_ditanium", "petrochem_inert"), Is.False);
            Assert.That(catalog.Matches("copper_polysteel", "steel"), Is.False);
            Assert.That(catalog.Matches("petrochem_inert", "petrochem_inert"), Is.True);
        }
        [Test]
        public void OutgoingPacketsMatchCore3Callbacks()
        {
            var add = ObjectControllerPacket.ReadFrom(new ByteReader(Bytes(w => new CraftingAddIngredientPacket(1, 2, 3, 4).WriteTo(w))));
            Assert.That(add.ControllerType, Is.EqualTo(0x107)); Assert.That(add.Flags, Is.EqualTo(0x23));
            var r = new ByteReader(add.Payload); Assert.That(r.U64(), Is.EqualTo(2)); Assert.That(r.U32(), Is.EqualTo(3)); Assert.That(r.U32(), Is.Zero); Assert.That(r.Byte(), Is.EqualTo(4)); ReplicationReader.RequireEnd(r);
            var remove = ObjectControllerPacket.ReadFrom(new ByteReader(Bytes(w => new CraftingRemoveIngredientPacket(1, 2, 3, 4).WriteTo(w))));
            Assert.That(remove.ControllerType, Is.EqualTo(0x108)); Assert.That(remove.Flags, Is.EqualTo(0x23));
            r = new ByteReader(remove.Payload); Assert.That(r.U32(), Is.EqualTo(3)); Assert.That(r.U64(), Is.EqualTo(2)); Assert.That(r.Byte(), Is.EqualTo(4)); ReplicationReader.RequireEnd(r);
            var experiment = ObjectControllerPacket.ReadFrom(new ByteReader(Bytes(w => new CraftingExperimentPacket(1, 5, new[] { (0, 2), (3, 1) }).WriteTo(w))));
            r = new ByteReader(experiment.Payload); Assert.That(r.Byte(), Is.EqualTo(5)); Assert.That(r.U32(), Is.EqualTo(2)); Assert.That(r.U32(), Is.Zero); Assert.That(r.U32(), Is.EqualTo(2)); Assert.That(r.U32(), Is.EqualTo(3)); Assert.That(r.U32(), Is.EqualTo(1)); ReplicationReader.RequireEnd(r);
            var custom = ObjectControllerPacket.ReadFrom(new ByteReader(Bytes(w => new CraftingCustomizationPacket(1, "Dagger", 225).WriteTo(w))));
            r = new ByteReader(custom.Payload); Assert.That(r.Unicode(), Is.EqualTo("Dagger")); Assert.That(r.Byte(), Is.EqualTo(255)); Assert.That(r.U32(), Is.EqualTo(225)); Assert.That(r.Byte(), Is.Zero); ReplicationReader.RequireEnd(r);
        }
        [Test]
        public void PlayerCraftingUpdatesPreserveFoodAndStationPermissions()
        {
            var r = new ByteReader(Bytes(w =>
            {
                w.Write((ushort)4); // One owner field is written manually by Core3 and omitted from its count.
                w.Write((ushort)1); w.Write(3);
                w.Write((ushort)2); w.Write(3);
                w.Write((ushort)3); w.Write(91ul);
                w.Write((ushort)5); w.Write(11);
                w.Write((ushort)10); w.Write(42);
            }));
            Assert.That(PlayerView9.TryReadDelta(r, out var update), Is.True);
            var playerRegistry = new SceneObjectRegistry();
            var currentPlayer = new CurrentPlayer(playerRegistry);
            var state = new WorldState(playerRegistry); update.Details?.Invoke(currentPlayer);
            var craftingProgress = new CraftingProgress(); update.Crafting?.Invoke(craftingProgress);
            Assert.That(craftingProgress.ExperimentationEnabled, Is.True); Assert.That(craftingProgress.ServerStage, Is.EqualTo(3));
            Assert.That(craftingProgress.ExperimentationPoints, Is.EqualTo(11)); Assert.That(craftingProgress.StationSwgId, Is.EqualTo(new SwgId(91)));
            Assert.That(currentPlayer.CharacterSheet.Food, Is.EqualTo(42));
        }
        [Test]
        public void SchematicSlotAndExperimentDeltasApplyWithoutSharingSnapshotLists()
        {
            var item = new ManufactureSchematicSceneObject(); item.IngredientQuantities.Add(new List<int>());
            var update = new ManufactureSchematicSceneObjectUpdate();
            var r = new ByteReader(Bytes(w => { w.Write(1); w.Write(1); w.Write((byte)2); w.Write((ushort)0); w.Write(1); w.Write(9); }));
            Assert.That(ManufactureSchematicView7.ReadDelta(3, r, update), Is.True); ReplicationReader.RequireEnd(r);
            r = new ByteReader(Bytes(w => { w.Write(1); w.Write(1); w.Write((byte)1); w.Write((ushort)0); Ascii(w, "crafting"); w.Write(0); Ascii(w, "damage"); }));
            ManufactureSchematicView7.ReadDelta(8, r, update); ReplicationReader.RequireEnd(r);
            r = new ByteReader(Bytes(w => { w.Write(1); w.Write(1); w.Write((byte)1); w.Write((ushort)0); w.Write(.11f); }));
            ManufactureSchematicView7.ReadDelta(9, r, update); ReplicationReader.RequireEnd(r); item.Apply(update);
            Assert.That(item.IngredientQuantities[0].Single(), Is.EqualTo(9)); Assert.That(item.ExperimentNames.Single(), Is.EqualTo("@crafting:damage")); Assert.That(item.ExperimentValues.Single(), Is.EqualTo(.11f));
            var snapshot = (ManufactureSchematicSceneObject)item.Copy(); item.ExperimentValues[0] = .5f; item.IngredientQuantities[0][0] = 20;
            Assert.That(snapshot.ExperimentValues[0], Is.EqualTo(.11f)); Assert.That(snapshot.IngredientQuantities[0][0], Is.EqualTo(9));
        }
        [Test]
        public void Core3UndercountedSlotDeltaStillAppliesTheWholeUpdate()
        {
            var reader = new ByteReader(Bytes(w =>
            {
                w.Write((ushort)1); // Core3 counts only startUpdate(7), not its four insertShort calls.
                w.Write((ushort)1); w.Write(1); w.Write(1); w.Write((byte)2); w.Write((ushort)0); w.Write(4);
                w.Write((ushort)2); w.Write(1); w.Write(1); w.Write((byte)2); w.Write((ushort)0); w.Write(1); w.Write(5ul);
                w.Write((ushort)3); w.Write(1); w.Write(1); w.Write((byte)2); w.Write((ushort)0); w.Write(1); w.Write(9);
                w.Write((ushort)5); w.Write(1); w.Write(1); w.Write((byte)2); w.Write((ushort)0); w.Write(0);
                w.Write((ushort)7); w.Write((byte)1);
            }));
            var item = new ManufactureSchematicSceneObject();
            item.IngredientSwgIds.Add(new List<SwgId>());
            item.IngredientQuantities.Add(new List<int>());
            item.Apply(ManufactureSchematicView7.ReadDeltaPacket(reader));
            Assert.That(item.IngredientSwgIds[0].Single(), Is.EqualTo(new SwgId(5)));
            Assert.That(item.IngredientQuantities[0].Single(), Is.EqualTo(9));
            ReplicationReader.RequireEnd(reader);
        }

        [Test]
        public async Task SharedDraftCatalogCachesMetadataAndDeduplicatesServerRequests()
        {
            var settings = ClientSettings.From(Path.Combine(Directory.GetParent(Application.dataPath).FullName, "client.ini"));
            using var assets = new TreRuntimeAssets(settings);
            await assets.Ready;
            var playerRegistry = new SceneObjectRegistry();
            var currentPlayer = new CurrentPlayer(playerRegistry);
            var state = new WorldState(playerRegistry);
            state.Templates = new ObjectTemplateCatalog(assets.Loader);
            var localizer = new StringTableLocalizer(assets);
            var sent = new List<string>();
            using var catalog = new DraftSchematicCatalog(state, assets, new SceneObjectNameResolver(assets, localizer), localizer, (name, args, target) => sent.Add(name), currentPlayer);
            string path = assets.Loader.Catalog().Keys.First(value => value.StartsWith("object/draft_schematic/") && value.Contains("dagger"));
            uint crc = ObjectTemplateNames.TemplateCrc(path);
            var draft = catalog.Get(crc);
            Assert.That(draft, Is.SameAs(catalog.Get(crc)));
            Assert.That(draft.PreviewPath, Does.Contain("dagger"));
            catalog.RequestDetails(crc);
            catalog.RequestDetails(crc);
            Assert.That(sent, Is.EqualTo(new[] { "requestDraftSlotsBatch", "requestResourceWeightsBatch" }));
            currentPlayer.BeginScene(1);
            state.BeginScene(Vector3.zero, "terrain/corellia.trn");
            catalog.RequestDetails(crc);
            Assert.That(sent.Count, Is.EqualTo(4), "Entering a new server scene permits fresh requests.");
        }
        [Test]
        public void ExperimentPointsAreConsumedFromTheTopOfTheStack()
        {
            var root = new GameObject("Point stack test", typeof(RectTransform), typeof(CanvasRenderer));
            try
            {
                var meter = root.AddComponent<CraftingExperimentMeter>();
                meter.color = Color.green;
                ((RectTransform)root.transform).sizeDelta = new Vector2(60, 160);
                meter.ShowPoints(10, 11);
                using var mesh = new UnityEngine.UI.VertexHelper();
                typeof(CraftingExperimentMeter).GetMethod("OnPopulateMesh", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly).Invoke(meter, new object[] { mesh });
                Assert.That(mesh.currentVertCount, Is.EqualTo(44), "Spent points keep their position in the stack.");
                var bottom = new UIVertex();
                var top = new UIVertex();
                mesh.PopulateUIVertex(ref bottom, 0);
                mesh.PopulateUIVertex(ref top, 40);
                Assert.That(bottom.color, Is.EqualTo((Color32)Color.green));
                Assert.That(top.color, Is.EqualTo((Color32)meter.inactiveColor));
                Assert.That(top.position.y, Is.GreaterThan(bottom.position.y));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        [Test]
        public void CraftingUsesScalableProjectVerdanaWithoutBitmapMaterials()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/SwgAssets/UI/FrontendFont.asset");
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/UI/Crafting" }))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                foreach (var text in prefab.GetComponentsInChildren<TMP_Text>(true))
                {
                    Assert.That(text.font, Is.SameAs(font), text.name);
                    Assert.That(text.fontSharedMaterial.shader.name, Does.Contain("Distance Field"), text.name);
                }
            }
        }
        [Test]
        public void CraftingVisualOverridesPreserveSharedPreviewAndExamineDefaults()
        {
            var view = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Crafting/Crafting.prefab").GetComponent<CraftingWindow>();
            var shared = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Interactions/ExamineWindow.prefab").GetComponent<ExamineWindow>();
            Assert.That(view.theme.name, Is.EqualTo("CraftingTheme"));
            Assert.That(shared.preview.framingPadding, Is.EqualTo(1.15f));
            Assert.That(shared.details.groupIndent, Is.Zero);
            Assert.That(view.experimentation.preview.framingPadding, Is.Not.EqualTo(shared.preview.framingPadding));
        }
        [Test]
        public void CraftingWindowDefaultsMatchTheReferenceWindowDimensions()
        {
            var view = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Crafting/Crafting.prefab").GetComponent<CraftingWindow>();
            Assert.That(((RectTransform)view.drafts.root.transform).sizeDelta, Is.EqualTo(new Vector2(850, 635)));
            Assert.That(((RectTransform)view.assembly.root.transform).sizeDelta, Is.EqualTo(new Vector2(1355, 724)));
            Assert.That(((RectTransform)view.summary.root.transform).sizeDelta, Is.EqualTo(new Vector2(562, 460)));
            Assert.That(((RectTransform)view.experimentation.root.transform).sizeDelta, Is.EqualTo(new Vector2(516, 535)));
            Assert.That(((RectTransform)view.customization.root.transform).sizeDelta, Is.EqualTo(new Vector2(784, 520)));
            Assert.That(view.pointStack.blockTexture, Is.Not.Null);
        }
        [Test]
        public void WorkflowPrefabProvidesAllSixEditableStepsAndSharedContainerWidgets()
        {
            var view = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Crafting/Crafting.prefab").GetComponent<CraftingWindow>();
            foreach (var step in new[] { view.drafts, view.assembly, view.summary, view.finish, view.experimentation, view.customization }) Assert.That(step.root, Is.Not.Null);
            Assert.That(view.practice.isOn, Is.False, "Creating a prototype must produce an item unless practice is explicitly selected.");
            Assert.That(view.slotPrefab.icon, Is.Not.Null);
            Assert.That(view.experimentPrefab.progress, Is.Not.Null);
            Assert.That(view.summaryPrefab.progress, Is.Not.Null);
            Assert.That(view.atlas, Is.Not.Null); Assert.That(view.itemPrefab, Is.Not.Null); Assert.That(view.slotPrefab, Is.Not.Null);
            Assert.That(view.experimentButton, Is.Not.Null); Assert.That(view.manufacturingButton, Is.Not.Null);
            Assert.That(view.UiPrefabs.Count(), Is.EqualTo(7));
            Assert.That(view.folderPrefab, Is.Not.Null);
            Assert.That(view.pointStack, Is.Not.Null);
            Assert.That(view.pointStack.GetComponent<CanvasRenderer>(), Is.Not.Null);
            Assert.That(view.riskAnimation.GetComponent<CanvasRenderer>(), Is.Not.Null);
            Assert.That(view.riskAnimation.animateRisk, Is.True);
            Assert.That(view.experimentPrefab.potential, Is.Not.Null);
            Assert.That(view.summaryRows.GetComponentInParent<UnityEngine.UI.ScrollRect>(true), Is.Not.Null);
            foreach (var step in new[] { view.drafts, view.assembly, view.summary, view.finish, view.experimentation, view.customization })
            {
                var right = step.root.transform.Find("WindowHeader/Drag handle/Right label").GetComponent<TMPro.TMP_Text>();
                Assert.That(right.text, Does.StartWith("STEP "));
                var header = step.root.transform.Find("WindowHeader/Drag handle").GetComponent<TreTextureBinding>();
                Assert.That(right.rectTransform.offsetMax.x, Is.LessThanOrEqualTo(-header.spriteBorder.z + 8), "Step labels must end on the bright header, before the ramp.");
            }
        }
    }
}

