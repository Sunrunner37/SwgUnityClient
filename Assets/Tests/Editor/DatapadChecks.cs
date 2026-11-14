using Swg.Client.Core;
using System;
using System.IO;
using System.Collections;
using UnityEngine.TestTools;
using Swg.Client.Runtime.Assets.Tre;
using Swg.Client.Runtime.Localization;
using Swg.Client.Runtime.Networking;
using System.Collections.Generic;
using TMPro;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Swg.Client.Core.Assets.Tre;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Assets.Formats.ObjectTemplates;
using Swg.Client.Core.Assets.Appearances;
using Swg.Client.Core.Configuration;
using Swg.Client.Core.Networking.Game.Replication;
using Swg.Client.Core.World;
using Swg.Client.Core.World.SceneObjects;
using Swg.Client.Core.Assets;
using Swg.Client.Core.Networking.Game.Packets.Data;
using Swg.Client.Core.Networking.Game.Packets;
using Swg.Client.Core.Networking.Game.Packets.Crafting;
using Swg.Client.Runtime.UI.Inventory;
using Swg.Client.Runtime.UI.Datapad;
using Swg.Client.Runtime.UI.Containers;

namespace Swg.Client.Tests.Editor
{
    public sealed class DatapadChecks
    {
        [Test]
        public void ExperimentsMatchNativePositionsGroupAndDeduplicateBeforeRendering()
        {
            var attributes = new[]
            {
                new SharedDraftSchematicTemplate.Attribute("complexity", ""),
                new SharedDraftSchematicTemplate.Attribute("charge", "charges"),
                new SharedDraftSchematicTemplate.Attribute("duplicate charge", "charges"),
                new SharedDraftSchematicTemplate.Attribute("power", "effectiveness")
            };
            var weights = new[]
            {
                new DraftSchematicDetails.ExperimentWeight("Overall quality", 2),
                new DraftSchematicDetails.ExperimentWeight("Unit toughness", 1)
            };
            var details = new DraftSchematicDetails { Experiments = new[] { Array.Empty<DraftSchematicDetails.ExperimentWeight>(), weights, weights, weights } };
            var groups = details.GroupExperiments(attributes);
            Assert.That(groups.Select(group => group.Name), Is.EqualTo(new[] { "charges", "effectiveness" }));
            Assert.That(groups[0].Properties.Select(property => property.Name), Is.EqualTo(new[] { "charge" }));
            Assert.That(groups[1].Properties[0].Name, Is.EqualTo("power"));
            Assert.That(groups[0].Properties[0].Weights, Is.EqualTo(new[] { ("Overall quality", 66), ("Unit toughness", 33) }));
        }
        [Test]
        public void ExperimentWeightsSurviveEitherReplyOrder()
        {
            var state = new WorldState(new SceneObjectRegistry());
            var weights = new[] { new[] { new DraftSchematicDetails.ExperimentWeight("Overall quality", 2), new DraftSchematicDetails.ExperimentWeight("Unit toughness", 1) } };
            state.UpdateDraftExperiments(123, weights);
            state.UpdateDraftDetails(123, new DraftSchematicDetails { Complexity = 7, Size = 4 });
            Assert.That(state.DraftDetails[123].Experiments, Is.SameAs(weights));
            Assert.That(state.DraftDetails[123].Complexity, Is.EqualTo(7));
            state.UpdateDraftExperiments(123, weights);
            Assert.That(state.DraftDetails[123].Size, Is.EqualTo(4));
        }
        [Test, Category("NativeAssets")]
        public void NativeDraftAttributesRetainExperimentLabelsAndOrder()
        {
            var settings = ClientSettings.From(Path.Combine(Directory.GetParent(Application.dataPath).FullName, "client.ini"));
            using var loader = new TreAssetLoader(settings.TreDirectory, settings.TreArchivePriorities);
            var attributes = SharedDraftSchematicTemplate.Attributes(loader.Read("object/draft_schematic/weapon/shared_rifle_t21.iff"));
            Assert.That(attributes[0].Name, Is.EqualTo("@crafting:complexity"));
            Assert.That(attributes[0].Experiment, Is.Empty);
            Assert.That(attributes[2].Name, Is.EqualTo("@crafting:minDamage"));
            Assert.That(attributes[2].Experiment, Is.EqualTo("@crafting:expDamage"));
        }
        [Test]
        public void WaypointPrefabUsesNativeActivePairsAndRoundThumbnails()
        {
            var item = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Datapad/DatapadItem.prefab").GetComponent<ContainerItem>();
            Assert.That(item.activeIndicator.activeSelf, Is.False);
            var arrows = item.activeIndicator.GetComponentsInChildren<RectTransform>(true).Skip(1).ToArray();
            Assert.That(arrows.Length, Is.EqualTo(2));
            Assert.That(arrows[0].localRotation, Is.EqualTo(Quaternion.identity));
            Assert.That(arrows[1].localRotation, Is.EqualTo(Quaternion.identity));
            Assert.That(((TreTexture)arrows[0].GetComponent<TreTextureBinding>().asset).sourceRect, Is.EqualTo(new RectInt(140, 39, 24, 26)));
            Assert.That(((TreTexture)arrows[1].GetComponent<TreTextureBinding>().asset).sourceRect, Is.EqualTo(new RectInt(181, 39, 24, 26)));
            Assert.That(item.image.rectTransform.sizeDelta.x, Is.EqualTo(item.image.rectTransform.sizeDelta.y));
        }
        [Test]
        public void ExperimentPacketKeepsBatchAndWeightedResourcePropertiesSeparate()
        {
            var payload = Bytes(writer =>
            {
                writer.Write(123u); writer.Write(123u);
                writer.Write((byte)1); writer.Write((byte)2); writer.Write((byte)0x81); writer.Write((byte)0xa1);
                writer.Write((byte)1); writer.Write((byte)2); writer.Write((byte)0x82); writer.Write((byte)0xa1);
            });
            var packet = DraftSchematicResourceWeightsPacket.ReadFrom(new ObjectControllerPacket(11, DraftSchematicResourceWeightsPacket.ControllerType, 42, payload));
            Assert.That(packet.BatchWeights[0].Weights, Is.EqualTo(new byte[] { 0x81, 0xa1 }));
            Assert.That(packet.Weights[0].Weights, Is.EqualTo(new byte[] { 0x82, 0xa1 }));
        }
        [UnityTest, Category("NativeAssets")]
        public IEnumerator PoiWaypointsShareActivationWithoutSendingServerIds()
        {
            var settings = ClientSettings.From(Path.Combine(Directory.GetParent(Application.dataPath).FullName, "client.ini"));
            using var assets = new TreRuntimeAssets(settings);
            yield return new WaitUntil(() => assets.Ready.IsCompleted);
            var state = new WorldState(new SceneObjectRegistry());
            state.BeginScene(7, Vector3.zero, "terrain/corellia.trn");
            var sent = new List<(string Name, SwgId Target)>();
            var localizer = new StringTableLocalizer(assets);
            using var datapad = new ClientDatapad(state, assets, new SceneObjectNameResolver(assets, localizer), localizer,
                (name, arguments, target) => sent.Add((name, target)));
            yield return new WaitUntil(() => datapad.Loading.IsCompleted);
            Assert.That(datapad.Loading.IsFaulted, Is.False);
            var entry = datapad.Entries(ClientDatapad.Tab.PointsOfInterest).First();
            var point = entry.Waypoint.Value;
            Assert.That(point.SwgId.IsNone, Is.True);
            Assert.That(point.Position, Is.Not.EqualTo(Vector3.zero));
            Assert.That(point.Color, Is.EqualTo(WaypointColor.Yellow));
            Assert.That(point.Active, Is.False);
            datapad.ToggleWaypoint(point);
            Assert.That(state.PoiWaypoints[entry.Key].Active, Is.True);
            Assert.That(state.NavigationWaypoints.Any(waypoint => waypoint.PoiKey == entry.Key && waypoint.Active), Is.True);
            Assert.That(datapad.Entries(ClientDatapad.Tab.PointsOfInterest).First().Waypoint.Value.Active, Is.True);
            Assert.That(sent, Is.Empty);
            datapad.ToggleWaypoint(state.PoiWaypoints[entry.Key]);
            Assert.That(state.PoiWaypoints[entry.Key].Active, Is.False);

            state.UpdateWaypoints(list => list[42] = new Waypoint(42, Vector3.one, state.PlanetCrc, "Home", WaypointColor.Blue, true));
            datapad.SingleActiveWaypoint = true;
            datapad.ToggleWaypoint(state.PoiWaypoints[entry.Key]);
            Assert.That(sent, Is.EqualTo(new[] { ("setWaypointActiveStatus", (SwgId)42) }));
            Assert.That(state.PoiWaypoints[entry.Key].Active, Is.True);
            // Apply the server's acknowledgement, then switch back to the player waypoint.
            state.UpdateWaypoints(list => list[42] = new Waypoint(42, Vector3.one, state.PlanetCrc, "Home", WaypointColor.Blue, false));
            sent.Clear();
            datapad.ToggleWaypoint(state.Waypoints[42]);
            Assert.That(state.PoiWaypoints[entry.Key].Active, Is.False);
            Assert.That(sent, Is.EqualTo(new[] { ("setWaypointActiveStatus", (SwgId)42) }));
            Assert.That(datapad.Entries(ClientDatapad.Tab.Waypoints).Count(), Is.EqualTo(1));
            Assert.That(datapad.UsedCapacity, Is.Zero);
        }
        static byte[] Bytes(Action<BinaryWriter> write)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            write(writer);
            return stream.ToArray();
        }
        [Test]
        public void PlayerBaselineRetainsGrantedSchematicCrcsAlongsideAbilities()
        {
            var bytes = Bytes(writer =>
            {
                writer.Write(0u); writer.Write(1u); // Abilities.
                writer.Write(new byte[16]);
                writer.Write(2u); writer.Write(1u);
                writer.Write(123u); writer.Write(123u);
                writer.Write(456u); writer.Write(456u);
                writer.Write(new byte[64]);
            });
            var update = PlayerView9.ReadBaseline(new ByteReader(bytes));
            var state = new WorldState(new SceneObjectRegistry());
            state.UpdateDraftSchematics(update.Schematics);
            Assert.That(state.DraftSchematics, Is.EqualTo(new uint[] { 123, 456 }));
        }
        [Test]
        public void SchematicDeltaPreservesVectorOrderAndDoesNotChangeAbilities()
        {
            var bytes = Bytes(writer =>
            {
                writer.Write((ushort)1); writer.Write((ushort)4);
                writer.Write(3u); writer.Write(2u);
                writer.Write((byte)0); writer.Write((ushort)0);
                writer.Write((byte)1); writer.Write((ushort)1); writer.Write(30u); writer.Write(30u);
                writer.Write((byte)2); writer.Write((ushort)0); writer.Write(40u); writer.Write(40u);
            });
            Assert.That(PlayerView9.TryReadDelta(new ByteReader(bytes), out var update), Is.True);
            Assert.That(update.Abilities, Is.Null);
            var state = new WorldState(new SceneObjectRegistry());
            state.UpdateDraftSchematics(list => list.AddRange(new uint[] { 10, 20 }));
            state.UpdateDraftSchematics(update.Schematics);
            Assert.That(state.DraftSchematics, Is.EqualTo(new uint[] { 40, 30 }));
            state.BeginScene(1, Vector3.zero, "terrain/tatooine.trn");
            Assert.That(state.DraftSchematics, Is.Empty);
        }
        [Test]
        public void SchematicClearDoesNotConsumeAnIndex()
        {
            var bytes = Bytes(writer =>
            {
                writer.Write((ushort)1); writer.Write((ushort)4);
                writer.Write(1u); writer.Write(3u); writer.Write((byte)4);
            });
            Assert.That(PlayerView9.TryReadDelta(new ByteReader(bytes), out var update), Is.True);
            var list = new List<uint> { 99 };
            update.Schematics(list);
            Assert.That(list, Is.Empty);
        }
        [Test]
        public void DatapadReusesContainerWidgetsAndHasFourEditableTabs()
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Datapad/Datapad.prefab");
            var window = root.GetComponent<DatapadWindow>();
            var inventory = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Inventory/Inventory.prefab").GetComponent<InventoryWindow>();
            Assert.That(window.tabs.Length, Is.EqualTo(4));
            Assert.That(window.itemPrefab, Is.TypeOf<ContainerItem>());
            Assert.That(window.capacityBar.GetType(), Is.EqualTo(inventory.capacityBar.GetType()));
            Assert.That(window.previews.GetType(), Is.EqualTo(inventory.previews.GetType()));
            Assert.That(window.previews.stagePosition, Is.Not.EqualTo(inventory.previews.stagePosition));
            Assert.That(root.GetComponentsInChildren<TMP_Text>(true).Any(t => t.text == "New Waypoint" || t.text == "Change View"), Is.False);
            var hud = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Hud/WorldHud.prefab");
            var button = hud.transform.Find("ButtonBar").GetChild(3).GetComponent<Button>();
            Assert.That(button.onClick.GetPersistentMethodName(0), Is.EqualTo("ToggleOpen"));
        }
        [Test]
        public void ManufacturingBaselinesRetainNameSizeAndCraftedDraftReference()
        {
            var bytes = Bytes(writer =>
            {
                writer.Write(12f);
                PacketFieldCodec.Ascii(writer, "craft_weapon_ingredients_n");
                writer.Write(0u);
                PacketFieldCodec.Ascii(writer, "rifle");
                PacketFieldCodec.Unicode(writer, "My Rifle Schematic");
                writer.Write(4u); writer.Write(25u);
                writer.Write(0u); writer.Write(1u);
                PacketFieldCodec.Unicode(writer, "Trixie");
                writer.Write(25u); writer.Write(8f);
            });
            var reader = new ByteReader(bytes);
            var update = ManufactureSchematicView.ReadBaseline3(reader);
            ReplicationReader.RequireEnd(reader);
            var schematic = new ManufactureSchematicSceneObject();
            schematic.Apply(update);
            var reference = Bytes(writer => { writer.Write(0x76u); writer.Write(0u); writer.Write(123u); writer.Write((ushort)1); });
            reader = new ByteReader(reference);
            schematic.Apply(ManufactureSchematicView.ReadBaseline6(reader));
            ReplicationReader.RequireEnd(reader);
            Assert.That(schematic.Name, Is.EqualTo("My Rifle Schematic"));
            Assert.That(schematic.DataSize, Is.EqualTo(4));
            Assert.That(schematic.DraftSchematicCrc, Is.EqualTo(123));
        }
        [Test, Category("NativeAssets")]
        public void IntangibleCreationDoesNotRequireASpatialModel()
        {
            var settings = ClientSettings.From(Path.Combine(Directory.GetParent(Application.dataPath).FullName, "client.ini"));
            using var loader = new TreAssetLoader(settings.TreDirectory, settings.TreArchivePriorities);
            var state = new WorldState(new SceneObjectRegistry()) { Templates = new ObjectTemplateCatalog(loader) };
            using var replication = GameServicesForTests.CreateReplication(state);
            replication.Commit(42, new ReplicationLifecycleUpdate(templateCrc: ObjectTemplateNames.TemplateCrc("object/intangible/pet/shared_r2.iff")),
                initialMovement: new MovementUpdate(Vector3.one, Quaternion.identity));
            replication.ApplyPending();
            Assert.That(state.SceneObjects.TryGet(42, out var sceneObject), Is.True);
            Assert.That(sceneObject, Is.TypeOf<IntangibleSceneObject>());
        }
        [UnityTest, Category("NativeAssets")]
        public IEnumerator CapacityCountsStoredDataSizeWithoutChargingForWaypoints()
        {
            var settings = ClientSettings.From(Path.Combine(Directory.GetParent(Application.dataPath).FullName, "client.ini"));
            using var assets = new TreRuntimeAssets(settings);
            yield return new WaitUntil(() => assets.Ready.IsCompleted);
            var state = new WorldState(new SceneObjectRegistry());
            state.BeginScene(1, Vector3.zero, "terrain/corellia.trn");
            state.Templates = new ObjectTemplateCatalog(assets.Loader);
            state.SceneObjects.Add(new SceneObject { SwgId = 1 });
            state.SceneObjects.Add(new TangibleSceneObject { SwgId = 2, ParentSwgId = 1, TemplatePath = "object/tangible/datapad/shared_character_datapad.iff" });
            state.SceneObjects.Add(new ManufactureSchematicSceneObject { SwgId = 3, ParentSwgId = 2, DataSize = 4 });
            state.SceneObjects.Add(new SceneObject { SwgId = 4, ParentSwgId = 2 });
            var localizer = new StringTableLocalizer(assets);
            using var datapad = new ClientDatapad(state, assets, new SceneObjectNameResolver(assets, localizer), localizer, (name, arguments, target) => { });
            yield return new WaitUntil(() => datapad.Loading.IsCompleted);
            Assert.That(datapad.Loading.IsFaulted, Is.False);
            state.UpdateWaypoints(list => list.Add(5, new Waypoint(5, Vector3.zero, state.PlanetCrc, "Home", WaypointColor.Blue, true)));
            Assert.That(datapad.Capacity, Is.EqualTo(100));
            Assert.That(datapad.UsedCapacity, Is.EqualTo(5));
            state.SceneObjects.Reparent(3, 1);
            Assert.That(datapad.UsedCapacity, Is.EqualTo(1));
        }
        [Test, Category("NativeAssets")]
        public void DraftTemplateResolvesTheActualCraftedModel()
        {
            var settings = ClientSettings.From(Path.Combine(Directory.GetParent(Application.dataPath).FullName, "client.ini"));
            using var loader = new TreAssetLoader(settings.TreDirectory, settings.TreArchivePriorities);
            string path = SharedDraftSchematicTemplate.CraftedTemplate(loader.Read("object/draft_schematic/weapon/shared_rifle_t21.iff"));
            Assert.That(path, Is.Not.Null);
            Assert.That(path, Does.Not.StartWith("object/draft_schematic/"));
            Assert.That(AppearanceLoader.LoadDetailed(loader, path).Parts, Is.Not.Empty);
        }
    }
}
