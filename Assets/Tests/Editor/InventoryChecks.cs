using System;
using System.IO;
using System.Text;
using System.Linq;
using NUnit.Framework;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Data;
using Swg.Client.Core.Networking.Game.Replication;
using Swg.Client.Core;
using Swg.Client.Core.World;
using Swg.Client.Core.World.SceneObjects;
using Swg.Client.Core.Configuration;
using Swg.Client.Runtime.Assets.Tre;
using Swg.Client.Runtime.Localization;
using Swg.Client.Runtime.Networking;
using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;
using UnityEngine;
using UnityEditor;
using Swg.Client.Runtime.UI.Inventory;
using UnityEngine.UI;

namespace Swg.Client.Tests.Editor
{
    public sealed class InventoryChecks
    {
        static byte[] Bytes(Action<BinaryWriter> write)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            write(writer);
            return stream.ToArray();
        }
        static void Tangible(BinaryWriter writer)
        {
            writer.Write(1f);
            PacketFieldCodec.Ascii(writer, ""); writer.Write(0u); PacketFieldCodec.Ascii(writer, "");
            PacketFieldCodec.Unicode(writer, "");
            writer.Write(2u); writer.Write((ushort)0); writer.Write(0u); writer.Write(0u);
            writer.Write(0u); writer.Write(3u); writer.Write(0u); writer.Write(1000u); writer.Write((byte)1);
        }
        [Test]
        public void ResourceStackAndCurrencyBaselinesPopulateApplicationFields()
        {
            var bytes = Bytes(writer => { Tangible(writer); writer.Write(41800u); writer.Write(1234ul); });
            var reader = new ByteReader(bytes);
            var update = ResourceContainerView.ReadBaseline3(reader);
            ReplicationReader.RequireEnd(reader);
            var item = new TangibleSceneObject(); item.Apply(update);
            Assert.That(item.Volume, Is.EqualTo(2));
            Assert.That(item.Quantity, Is.EqualTo(41800));
            ResourceContainerView.ReadDelta3(11, new ByteReader(BitConverter.GetBytes(41000u)), update);
            item.Apply(update);
            Assert.That(item.Quantity, Is.EqualTo(41000));
            bytes = Bytes(writer => { writer.Write(11410u); writer.Write(125u); writer.Write(0u); writer.Write(0u); writer.Write(0u); writer.Write(0u); });
            reader = new ByteReader(bytes);update = CreatureView1.ReadBaseline(reader);ReplicationReader.RequireEnd(reader);
            var player = new CreatureSceneObject();player.Apply(update);
            Assert.That(player.BankCredits, Is.EqualTo(11410));Assert.That(player.CashCredits, Is.EqualTo(125));
            CreatureView1.ReadDeltaField(1, new ByteReader(BitConverter.GetBytes(250u)), update);player.Apply(update);
            Assert.That(player.CashCredits, Is.EqualTo(250));
        }
        [Test]
        public void InventoryUsesContainmentAndReflectsReparentingWithoutAnotherItemStore()
        {
            var settings = ClientSettings.From(Path.Combine(Directory.GetParent(Application.dataPath).FullName, "client.ini"));
            using var assets = new TreRuntimeAssets(settings);
            var registry = new SceneObjectRegistry();var state = new WorldState(registry);
            state.BeginScene(1, Vector3.zero, "terrain/tatooine.trn");state.Templates = new ObjectTemplateCatalog(assets.Loader);
            registry.Add(new CreatureSceneObject { SwgId = 1 });
            registry.Add(new TangibleSceneObject { SwgId = 2, ParentSwgId = 1, TemplatePath = "object/tangible/inventory/shared_character_inventory.iff" });
            registry.Add(new TangibleSceneObject { SwgId = 3, ParentSwgId = 2, Volume = 2 });
            registry.Add(new TangibleSceneObject { SwgId = 4, ParentSwgId = 1, TemplatePath = "object/tangible/wearables/armor/composite/shared_armor_composite_boots.iff" });
            SwgId opened = default;
            using var inventory = new ClientInventory(state, id => opened = id);
            inventory.Open();
            Assert.That(opened.Value, Is.EqualTo(2));Assert.That(inventory.Capacity, Is.EqualTo(80));Assert.That(inventory.UsedCapacity, Is.EqualTo(2));Assert.That(inventory.Items.Count, Is.EqualTo(2));
            registry.Reparent(3, 99);
            Assert.That(inventory.Items.Count, Is.EqualTo(1));Assert.That(inventory.UsedCapacity, Is.Zero);
            registry.Remove(4);Assert.That(inventory.Items, Is.Empty);
        }
        [Test]
        public void ResourceDisplayNamesUseNativeParentClasses()
        {
            var settings = ClientSettings.From(Path.Combine(Directory.GetParent(Application.dataPath).FullName, "client.ini"));
            using var assets = new TreRuntimeAssets(settings);
            var names = new SceneObjectNameResolver(assets, new StringTableLocalizer(assets));
            Assert.That(names.Resolve(new TangibleSceneObject { ResourceType = "aluminum_agrinium" }), Is.EqualTo("Aluminum"));
            Assert.That(names.Resolve(new TangibleSceneObject { ResourceType = "copper_beyrllius" }), Is.EqualTo("Copper"));
        }
        [Test]
        public void InventoryPrefabOwnsViewsAndTwelveSecondAtlasRotation()
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Inventory/Inventory.prefab");
            var window = root.GetComponent<InventoryWindow>();
            Assert.That(window.examinePanel.GetComponent<LayoutElement>().preferredWidth, Is.EqualTo(220));
            Assert.That(window.characterPanel.GetComponent<LayoutElement>().preferredWidth, Is.EqualTo(220));
            Assert.That(window.centerPanel.parent.GetComponent<HorizontalLayoutGroup>(), Is.Not.Null);
            Assert.That(window.grid.constraint, Is.EqualTo(GridLayoutGroup.Constraint.Flexible));
            Assert.That(window.details, Is.Not.Null);Assert.That(window.characterPreview, Is.Not.Null);
            Assert.That(window.previews.rotationSeconds, Is.EqualTo(12));
            Assert.That(window.previews.previewCamera.cullingMask, Is.EqualTo(1 << 30));
            Assert.That(window.previews.previewCamera.enabled, Is.False);
            Assert.That(root.GetComponentsInChildren<Camera>(true).Length, Is.EqualTo(2));
        }
    }
}

