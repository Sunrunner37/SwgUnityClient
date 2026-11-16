using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Swg.Client.Core;
using Swg.Client.Core.Assets;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Configuration;
using Swg.Client.Core.Characters.Animation;
using Swg.Client.Core.Networking.Game.Handlers;
using Swg.Client.Core.Networking.Game.Handlers.Combat;
using Swg.Client.Core.Networking.Game.Handlers.Effects;
using Swg.Client.Core.Networking.Game.Packets;
using Swg.Client.Core.Networking.Game.Packets.Combat;
using Swg.Client.Core.Networking.Game.Packets.Effects;
using Swg.Client.Core.Networking.Game.Replication;
using Swg.Client.Core.World;
using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;
using Swg.Client.Runtime.Assets.Tre;
using Swg.Client.Runtime.Networking;
using Swg.Client.Runtime.UI.Hud;

namespace Swg.Client.Tests.Editor
{
    public sealed class EntertainerChecks
    {
        static byte[] Bytes(Action<BinaryWriter> write)
        {
            using var stream = new MemoryStream(); using var writer = new BinaryWriter(stream);
            write(writer); return stream.ToArray();
        }
        static void Ascii(BinaryWriter writer, string value) { writer.Write((ushort)value.Length); writer.Write(System.Text.Encoding.ASCII.GetBytes(value)); }
        static ObjectControllerPacket Packet(uint type, ulong receiver, Action<BinaryWriter> payload) => ObjectControllerPacket.ReadFrom(new ByteReader(Bytes(writer =>
        {
            writer.Write(ObjectControllerPacket.OperandCount); writer.Write(ObjectControllerPacket.Opcode); writer.Write(0x1bu); writer.Write(type); writer.Write(receiver); writer.Write(0u); payload(writer);
        })));
        [Test]
        public void PerformanceAndListeningDeltasUpdateTheCreatureWithoutRebuildingItsAppearance()
        {
            var update = new CreatureSceneObjectUpdate();
            CreatureView6.ReadDeltaField(3, new ByteReader(Bytes(w => Ascii(w, "music_2"))), update);
            CreatureView6.ReadDeltaField(4, new ByteReader(Bytes(w => Ascii(w, "entertained"))), update);
            CreatureView6.ReadDeltaField(12, new ByteReader(BitConverter.GetBytes(4)), update);
            CreatureView4.ReadDeltaField(6, new ByteReader(BitConverter.GetBytes(42UL)), update);
            var creature = new CreatureSceneObject(); creature.Apply(update);
            Assert.That(creature.IsPlayingMusic, Is.True);
            Assert.That(creature.MoodAnimation, Is.EqualTo("entertained"));
            Assert.That(creature.ListenSwgId, Is.EqualTo(new SwgId(42)));
            Assert.That(update.Changes, Is.EqualTo(SceneObjectChange.Performance));
            creature.Apply(new CreatureSceneObjectUpdate { PerformanceType = 0, PerformanceAnimation = "", MoodAnimation = "", ListenSwgId = default(SwgId) });
            Assert.That(creature.IsPlayingMusic, Is.False);
            Assert.That(creature.ListenSwgId.IsNone, Is.True);
        }
        [TestCase((byte)113)]
        [TestCase((byte)114)]
        [TestCase((byte)115)]
        [TestCase((byte)116)]
        public void EntertainerRadialActionsStayServerOwned(byte action)
        {
            var state = GameServicesForTests.CreateState(out var player); player.BeginScene(11);
            var performer = new CreatureSceneObject { SwgId = 42, PerformanceType = 1, PerformanceAnimation = "music_2" };
            state.SceneObjects.Add(performer);
            Assert.That(ObjectMenuDefaults.Create(performer, (ObjectTypeId)0x409).Any(entry => entry.RadialId >= 113 && entry.RadialId <= 116), Is.False);
            var selections = new List<(ulong Target, byte Action)>();
            using var interactions = new ClientObjectInteractions(state, _ => { }, _ => { }, packet => selections.Add((packet.TargetSwgId.Value, packet.RadialId)), (_, _, _) => Assert.Fail("Core3 handles the server-provided radial action"), player);
            interactions.SelectMenuAction(42, action, 3);
            Assert.That(selections.Single(), Is.EqualTo((42UL, action)));
        }
        [Test]
        public void BuffHandlersApplyOnTheMainThreadRenewAndRemoveOnlyTheOwnersBuffs()
        {
            var state = GameServicesForTests.CreateState(out var player); player.BeginScene(11);
            using var replication = GameServicesForTests.CreateReplication(state, player);
            IObjectControllerHandler add = new AddBuffControllerHandler(replication);
            IObjectControllerHandler remove = new RemoveBuffControllerHandler(replication);
            uint crc = ObjectTemplateNames.TemplateCrc("performance_enhance_music_focus");
            add.Handle(Packet(AddBuffPacket.ControllerType, 11, w => { w.Write(crc); w.Write(60f); }));
            Assert.That(player.Buffs, Is.Empty); replication.ApplyPending();
            double original = player.Buffs[crc];
            Assert.That(original - Time.realtimeSinceStartupAsDouble, Is.InRange(59, 60));
            add.Handle(Packet(AddBuffPacket.ControllerType, 11, w => { w.Write(crc); w.Write(120f); })); replication.ApplyPending();
            Assert.That(player.Buffs.Count, Is.EqualTo(1)); Assert.That(player.Buffs[crc], Is.GreaterThan(original));
            remove.Handle(Packet(RemoveBuffPacket.ControllerType, 42, w => w.Write(crc))); replication.ApplyPending(); Assert.That(player.Buffs.Count, Is.EqualTo(1));
            remove.Handle(Packet(RemoveBuffPacket.ControllerType, 11, w => w.Write(crc))); replication.ApplyPending(); Assert.That(player.Buffs, Is.Empty);
            player.AddBuff(crc, 100); player.BeginScene(12); Assert.That(player.Buffs, Is.Empty);
        }
        [Test]
        public async Task NativeInstrumentLoopsFlourishesAndApplauseResolveFromTheStringSelectors()
        {
            using var assets = new TreRuntimeAssets(ClientSettings.From(Path.Combine(Application.dataPath, "../client.ini")));
            var clips = await assets.ReadAsync(loader =>
            {
                var skeleton = SkeletonData.Load(loader, "appearance/hum_f.sat");
                return (skeleton, loop: skeleton.ResolveAmbient("music_2", ""), flourish: skeleton.ResolveAction("skill_action_1", "", CreaturePosture.SkillAnimating, performance: "music_2"), applause: skeleton.ResolveAmbient("", "entertained"));
            });
            Assert.That(clips.loop, Is.Not.Null); Assert.That(clips.flourish, Is.Not.Null); Assert.That(clips.applause, Is.Not.Null);
            foreach (var clip in new[] { clips.loop, clips.flourish, clips.applause })
                Assert.That(clips.skeleton.Joints.Max(j => Quaternion.Angle(clip.Sample(j.Name, 0).Rotation, clip.Sample(j.Name, clip.Duration * .4f).Rotation)), Is.GreaterThan(5));
            var state = GameServicesForTests.CreateState(out _);
            using var combat = new ClientCombat(assets, state); await combat.Loading;
            string played = null; combat.Subscribe(42, (name, weapon, reaction) => played = name);
            IObjectControllerHandler handler = new FlourishControllerHandler(combat);
            handler.Handle(Packet(FlourishPacket.ControllerType, 42, w => w.Write(3)));
            await Task.Delay(30);
            Assert.That(played, Is.EqualTo("skill_action_3"));
        }
        [Test]
        public async Task BuffWindowSkinUsesTheNativeTranslucentFrameAndControls()
        {
            var root = PrefabUtility.LoadPrefabContents("Assets/Prefabs/UI/Hud/BuffHud.prefab");
            try
            {
                var hud = root.GetComponent<BuffHud>();
                var panel = (RectTransform)hud.panel.transform;
                Assert.That(panel.sizeDelta, Is.EqualTo(new Vector2(90, 160)));
                var close = panel.Find("Close").GetComponent<UnityEngine.UI.Button>();
                Assert.That(close.onClick.GetPersistentMethodName(0), Is.EqualTo("Close"));
                Assert.That(close.onClick.GetPersistentTarget(0), Is.EqualTo(hud));
                var scroll = hud.content.GetComponentInParent<UnityEngine.UI.ScrollRect>();
                Assert.That(scroll.verticalScrollbar, Is.Not.Null);
                Assert.That(scroll.horizontal, Is.False);
                var frame = (TrePanel)panel.Find("Frame").GetComponent<TreTextureBinding>().asset;
                Assert.That(frame.layers.Single().style, Is.EqualTo("Styles.New.tool.tool_TL_in"));
                Assert.That(panel.Find("Glass"), Is.Null);
                using var assets = new TreRuntimeAssets(ClientSettings.From(Path.Combine(Application.dataPath, "../client.ini")));
                var data = await assets.ReadAsync(loader => frame.Load(loader));
                Assert.That(data.Pixels[((data.Height / 2) * data.Width + data.Width / 2) * 4 + 3], Is.InRange(1, 254));
                Assert.That(data.Pixels.Where((value, index) => index % 4 == 3).Any(value => value > 0), Is.True);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        [Test]
        public async Task BuffHudUsesTheNativeMusicAndDanceArtworkInEditablePrefabs()
        {
            var hud = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Hud/WorldHud.prefab").GetComponentInChildren<BuffHud>(true);
            Assert.That(hud, Is.Not.Null); Assert.That(hud.enabled, Is.True); Assert.That(hud.iconPrefab.image, Is.Not.Null);
            Assert.That(hud.definitions.Count(value => value.name.StartsWith("performance_")), Is.EqualTo(3));
            using var assets = new TreRuntimeAssets(ClientSettings.From(Path.Combine(Application.dataPath, "../client.ini")));
            foreach (var definition in hud.definitions.Where(value => value.name.StartsWith("performance_")))
            {
                var data = await assets.ReadAsync(loader => definition.icon.Load(loader));
                Assert.That(data.Width, Is.EqualTo(24)); Assert.That(data.Height, Is.EqualTo(24));
                Assert.That(data.Pixels.Any(value => value != 0), Is.True);
            }
        }
    }
}
