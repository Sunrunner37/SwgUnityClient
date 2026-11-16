using Swg.Client.Runtime.Assets.Tre;
using Swg.Client.Runtime.UI;
using Swg.Client.Core.World;
using Swg.Client.Core.Networking.Game.Replication;
using Swg.Client.Core.Networking.Game.Handlers.Combat;
using Swg.Client.Core.Networking.Game.Handlers;
using Swg.Client.Core.Networking.Game.Packets.Combat;
using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;
using System.Threading;
using UnityEngine.TestTools;
using Swg.Client.Runtime.Networking;
using Swg.Client.Core.World.SceneObjects;
using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking.Game.Packets.Ui;
using Swg.Client.Runtime.UI.Navigation.Minimap;
using Swg.Client.Runtime.UI.Windows;
using Swg.Client.Runtime.UI.Screens.Loading;
using Swg.Client.Runtime.UI.Commands;

namespace Swg.Client.Tests.Editor
{
    public sealed class HudFeatureChecks
    {
        [Test]
        public void HoverCursorUsesServerCapabilitiesAndKeepsVehiclesExaminable()
        {
            var npc = new CreatureSceneObject();
            Assert.That(ClientCursor.ForObject(npc, (ObjectTypeId)0x400, false), Is.EqualTo(ClientCursor.WorldCursor.Examine));
            npc.PvpStatus = 1;
            Assert.That(ClientCursor.ForObject(npc, (ObjectTypeId)0x400, false), Is.EqualTo(ClientCursor.WorldCursor.Attack));
            npc.Options = 8;
            Assert.That(ClientCursor.ForObject(npc, (ObjectTypeId)0x400, false), Is.EqualTo(ClientCursor.WorldCursor.Talk));
            Assert.That(ClientCursor.ForObject(npc, (ObjectTypeId)0x10001, false), Is.EqualTo(ClientCursor.WorldCursor.Examine));
            npc.Options = 0;
            npc.Posture = CreaturePosture.Dead;
            Assert.That(ClientCursor.ForObject(npc, (ObjectTypeId)0x400, false), Is.EqualTo(ClientCursor.WorldCursor.Examine));
            Assert.That(ClientCursor.ForObject(new TangibleSceneObject(), (ObjectTypeId)0x4006, true), Is.EqualTo(ClientCursor.WorldCursor.Use));
        }

        [Test]
        public void ConversationFlagsReadBaselineAndDeltaAndSurvivePromotion()
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            writer.Write(1f);
            Ascii(writer, ""); writer.Write(0u); Ascii(writer, ""); writer.Write(0u);
            writer.Write(1u); // Volume.
            writer.Write((ushort)0); // Customization.
            writer.Write(0u); writer.Write(0u); // Visible components.
            writer.Write(8u); // CONVERSE.
            writer.Write(1u); writer.Write(0u); writer.Write(100u); writer.Write((byte)1);
            var update = TangibleView3.ReadBaseline(new ByteReader(stream.ToArray()));
            var tangible = new TangibleSceneObject();
            update.ApplyTo(tangible);
            Assert.That(tangible.CanConverse, Is.True);
            var creature = new CreatureSceneObject();
            tangible.CopyTo(creature);
            Assert.That(creature.CanConverse, Is.True);
            update = new TangibleSceneObjectUpdate();
            Assert.That(TangibleView3.ReadDeltaField(6, new ByteReader(new byte[4]), update), Is.True);
            update.ApplyTo(creature);
            Assert.That(creature.CanConverse, Is.False);
            Assert.That(update.Changes, Is.EqualTo(SceneObjectChange.State));
        }

        [Test]
        public void AttackabilityPacketAppliesThroughMainThreadReplication()
        {
            var state = GameServicesForTests.CreateState(out var player);
            var npc = new CreatureSceneObject { SwgId = 42 };
            state.SceneObjects.Add(npc);
            var replication = GameServicesForTests.CreateReplication(state, player);
            try
            {
                IGamePacketHandler handler = new UpdatePVPStatusPacketHandler(replication);
                byte[] Packet(uint flags)
                {
                    using var stream = new MemoryStream();
                    using var writer = new BinaryWriter(stream);
                    writer.Write((ushort)4); writer.Write(UpdatePVPStatusPacket.Opcode);
                    writer.Write(flags); writer.Write(0u); writer.Write(42ul);
                    return stream.ToArray();
                }
                handler.Handle(new ByteReader(Packet(1)));
                Assert.That(npc.CanAttack, Is.False, "Network handlers must queue rather than mutate the object.");
                replication.ApplyPending();
                Assert.That(npc.CanAttack, Is.True);
                handler.Handle(new ByteReader(Packet(0)));
                replication.ApplyPending();
                Assert.That(npc.CanAttack, Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(replication.gameObject); }
        }

        [Test]
        public void MinimapPlayerMarkerUsesTheBuiltInCircle()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Navigation/Minimap/Minimap.prefab");
            var marker = prefab.GetComponent<Minimap>().playerMarker.GetComponent<UnityEngine.UI.Image>();
            Assert.That(marker.sprite, Is.SameAs(AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd")));
            Assert.That(marker.color, Is.EqualTo(Color.white));
        }

        [Test]
        public void RepairQuoteUsesServerTextAndRespondsWithPageAndAcceptEvent()
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            writer.Write((ushort)2);
            writer.Write(SuiCreatePagePacket.Opcode);
            writer.Write(765u);
            Ascii(writer, "Script.listBox");
            writer.Write(3u);
            Property(writer, "bg.caption.lblTitle", "@pet/pet_menu:confirm_repairs_t");
            Property(writer, "Prompt.lblPrompt", "@pet/pet_menu:vehicle_repair_d");
            Property(writer, "List.dataList.0", "@pet/pet_menu:repair_cost_prompt 1900");
            writer.Write(1234ul);
            writer.Write(10f);
            writer.Write(0ul);
            var page = SuiCreatePagePacket.ReadFrom(new ByteReader(stream.ToArray())).Page;
            var previous = SynchronizationContext.Current;
            SuiEventNotificationPacket sent = default;
            SynchronizationContext.SetSynchronizationContext(new ImmediateContext());
            try
            {
                using var repairs = new ClientVehicleRepairs(packet => sent = packet);
                repairs.Receive(page);
                Assert.That(repairs.CurrentQuote.Title, Is.EqualTo("@pet/pet_menu:confirm_repairs_t"));
                Assert.That(repairs.CurrentQuote.Rows, Is.EqualTo(new[] { "@pet/pet_menu:repair_cost_prompt 1900" }));
                repairs.Respond(true);
                Assert.That(repairs.CurrentQuote, Is.Null);
                Assert.That(sent.PageId, Is.EqualTo(page.Id));
                Assert.That(sent.EventIndex, Is.Zero);
                Assert.That(sent.Arguments, Is.EqualTo(new[] { "-1", "@pet/pet_menu:confirm_repairs_t" }));
                repairs.Receive(page);
                repairs.Close(page.Id + 1);
                Assert.That(repairs.CurrentQuote, Is.Not.Null);
                repairs.Respond(false);
                Assert.That(sent.EventIndex, Is.EqualTo(1));
                repairs.Receive(page);
                var unrelated = stream.ToArray();
                var oldTitle = System.Text.Encoding.Unicode.GetBytes("@pet/pet_menu:confirm_repairs_t");
                // Same SUI list-box envelope, different title: this is not a repair confirmation.
                for (int i = 0; i <= unrelated.Length - oldTitle.Length; i++)
                    if (unrelated.Skip(i).Take(oldTitle.Length).SequenceEqual(oldTitle)) { unrelated[i + 2] = (byte)'X'; break; }
                LogAssert.Expect(LogType.Warning, "Unsupported server dialog: Script.listBox");
                repairs.Receive(SuiCreatePagePacket.ReadFrom(new ByteReader(unrelated)).Page);
                Assert.That(repairs.CurrentQuote.PageId, Is.EqualTo(page.Id));
                repairs.Close(page.Id);
                Assert.That(repairs.CurrentQuote, Is.Null);
            }
            finally { SynchronizationContext.SetSynchronizationContext(previous); }
            Assert.That(page.TargetSwgId, Is.EqualTo(1234));
            using var response = new MemoryStream();
            using var output = new BinaryWriter(response);
            new SuiEventNotificationPacket(page.Id, 0, "-1", "Confirm Vehicle Repairs").WriteTo(output);
            var reader = new ByteReader(response.ToArray());
            Assert.That(reader.U16(), Is.EqualTo(2));
            Assert.That(reader.U32(), Is.EqualTo(SuiEventNotificationPacket.Opcode));
            Assert.That(reader.U32(), Is.EqualTo(765));
            Assert.That(reader.U32(), Is.Zero);
            Assert.That(reader.U32(), Is.EqualTo(2));
            Assert.That(reader.U32(), Is.Zero);
            Assert.That(reader.Unicode(), Is.EqualTo("-1"));
            Assert.That(reader.Unicode(), Is.EqualTo("Confirm Vehicle Repairs"));
            Assert.That(reader.Remaining, Is.Zero);
        }
        static void Ascii(BinaryWriter writer, string value) { writer.Write((ushort)value.Length); writer.Write(System.Text.Encoding.ASCII.GetBytes(value)); }
        static void Unicode(BinaryWriter writer, string value) { writer.Write((uint)value.Length); writer.Write(System.Text.Encoding.Unicode.GetBytes(value)); }
        static void Property(BinaryWriter writer, string widget, string text)
        {
            writer.Write((byte)3); writer.Write(1u); Unicode(writer, text); writer.Write(2u); Ascii(writer, widget); Ascii(writer, "Text");
        }
        [Test]
        public void WearablesAndWeaponsUseEquipmentActivationButResourceStacksDoNot()
        {
            Assert.That(ClientInventory.CanEquip(new TangibleSceneObject { TemplatePath = "object/tangible/wearables/pants/shared_pants_s26.iff" }), Is.True);
            Assert.That(ClientInventory.CanEquip(new TangibleSceneObject { TemplatePath = "object/weapon/melee/knife/shared_knife_dagger.iff" }), Is.True);
            Assert.That(ClientInventory.CanEquip(new TangibleSceneObject { TemplatePath = "object/resource_container/shared_resource_container_mineral.iff" }), Is.False);
        }
        sealed class ImmediateContext : SynchronizationContext
        {
            public override void Post(SendOrPostCallback callback, object state) => callback(state);
        }
        [TestCase(32f, 128f)]
        [TestCase(64f, 256f)]
        public void MinimapProjectionMatchesCaptureAndClampsOnlyBeyondTheRing(float range, float size)
        {
            var owner = new GameObject("Minimap projection test");
            try
            {
                var camera = owner.AddComponent<Camera>();
                camera.enabled = false;
                camera.orthographic = true;
                camera.aspect = 1;
                camera.orthographicSize = range;
                camera.transform.SetPositionAndRotation(new Vector3(20, 300, 40), Quaternion.Euler(90, 0, 0));
                var point = new Vector3(20 + range * .25f, 0, 40);
                var viewport = camera.WorldToViewportPoint(point);
                var expected = new Vector2((viewport.x - .5f) * size, (viewport.y - .5f) * size);
                var projected = Minimap.Project(camera, point, Vector2.one * size, size * .4f);
                Assert.That(Vector2.Distance(projected, expected), Is.LessThan(.001));
                Assert.That(projected.x, Is.EqualTo(size * .125f).Within(.001));
                var distant = Minimap.Project(camera, new Vector3(10000, 0, 40), Vector2.one * size, size * .4f);
                Assert.That(distant.magnitude, Is.EqualTo(size * .4f).Within(.001));
            }
            finally { UnityEngine.Object.DestroyImmediate(owner); }
        }
        [Test]
        public void LoadingVariantsHaveScalableTextAndValidSerializedReferences()
        {
            foreach (var path in new[] { "Assets/Prefabs/LoadingThematicScreen.prefab", "Assets/Prefabs/LoadingHorizontalScreen.prefab" })
            {
                var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var view = root.GetComponentInChildren<LoadingScreenView>(true);
                Assert.That(view.title, Is.Not.Null, path);
                Assert.That(view.description, Is.Not.Null, path);
                Assert.That(view.status, Is.Not.Null, path);
                Assert.That(root.GetComponentsInChildren<UnityEngine.UI.Text>(true), Is.Empty, path);
                Assert.That(view.title.font, Is.Not.Null);
            }
        }
        [Test]
        public void ActionSlotsUseTheNativeToolbarArtwork()
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Commands/ActionBar.prefab");
            foreach (var slot in root.GetComponentsInChildren<CommandButton>(true))
            {
                Assert.That(slot.transform.Find("Opaque slot backing"), Is.Null);
                var binding = slot.GetComponent<TreTextureBinding>();
                Assert.That(binding.asset.name, Is.EqualTo("ActionSlot"));
                Assert.That(binding.spriteBorder, Is.EqualTo(Vector4.zero));
            }
        }
    }
}
