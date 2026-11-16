using Swg.Client.Core.Networking.Game.Packets;
using System;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Characters;
using Swg.Client.Core.Configuration;
using Swg.Client.Core.Networking.Game.Packets.Chat;
using Swg.Client.Core.Networking.Game.Packets.Data;
using Swg.Client.Core.Networking.Game.Packets.Effects;
using Swg.Client.Runtime.Assets.Tre;
using Swg.Client.Runtime.Localization;
using Swg.Client.Runtime.Networking;
using Swg.Client.Runtime.Networking.Chat;
using Swg.Client.Runtime.UI.Hud;
using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;

namespace Swg.Client.Tests.Editor
{
    public sealed class EntertainerProgressChecks
    {
        [Test]
        public void PulsesAndFlourishesBuildDurationAndStrengthButStoppingTooEarlyIsNotEligible()
        {
            var estimate = new EntertainerBuffEstimate(100);
            estimate.Advance(109, 7, true);
            Assert.That(estimate.Minutes, Is.Zero);
            estimate.Flourish(109, 7, true);
            estimate.Advance(110, 7, true);
            Assert.That(estimate.Minutes, Is.EqualTo(4));
            Assert.That(estimate.Strength, Is.EqualTo(14));
            Assert.That(estimate.MinimumReached(159.9), Is.False);
            Assert.That(estimate.MinimumReached(160), Is.True);
        }
        [Test]
        public void OnlyFiveFlourishesPerCycleContributeAndBothProgressValuesCap()
        {
            var estimate = new EntertainerBuffEstimate(0);
            for (int i = 0; i < 8; i++) estimate.Flourish(5, 7, true);
            Assert.That(estimate.Flourishes, Is.EqualTo(8));
            Assert.That(estimate.Minutes, Is.EqualTo(10));
            estimate.Flourish(10, 7, true);
            Assert.That(estimate.Minutes, Is.EqualTo(14));
            estimate.Advance(600, 7, true);
            Assert.That(estimate.Minutes, Is.EqualTo(120));
            Assert.That(estimate.Strength, Is.EqualTo(125));
            Assert.That(estimate.SecondsToFull(600, 7), Is.Zero);
        }
        [Test]
        public void UngroupedTimeDoesNotBuildProgressOrBackfillWhenGrouping()
        {
            var estimate = new EntertainerBuffEstimate(0);
            estimate.Advance(30, 7, false);
            estimate.Flourish(31, 7, false);
            estimate.Advance(40, 7, true);
            Assert.That(estimate.Minutes, Is.EqualTo(2));
            Assert.That(estimate.Strength, Is.EqualTo(7));
            Assert.That(estimate.SecondsToFull(40, 7), Is.GreaterThan(20));
        }
        [Test]
        public void NewListeningSessionDoesNotReuseThePreviousEstimate()
        {
            var old = new EntertainerBuffEstimate(0); old.Advance(600, 7, true);
            var fresh = new EntertainerBuffEstimate(600);
            Assert.That(fresh.Minutes, Is.Zero);
            Assert.That(fresh.Strength, Is.Zero);
            Assert.That(fresh.MinimumReached(601), Is.False);
        }
        static byte[] Bytes(Action<BinaryWriter> write)
        { using var stream = new MemoryStream(); using var writer = new BinaryWriter(stream); write(writer); return stream.ToArray(); }
        static ChatSystemPacket Watch(string key, ulong target = 42)
        {
            byte[] body = Bytes(w =>
            {
                w.Write((ushort)0); w.Write((byte)1); w.Write(uint.MaxValue);
                void Id(string file, string name) { PacketFieldCodec.Ascii(w, file); w.Write(0u); PacketFieldCodec.Ascii(w, name); }
                Id("performance", key);
                for (int i = 0; i < 3; i++) { w.Write(i == 1 ? target : 0UL); Id("", ""); PacketFieldCodec.Unicode(w, ""); }
                w.Write(0); w.Write(0f); w.Write((byte)0);
            });
            if ((body.Length & 1) != 0) { Array.Resize(ref body, body.Length + 1); body[0] = 1; }
            return ChatSystemPacket.ReadFrom(new ByteReader(Bytes(w =>
            {
                w.Write((ushort)4); w.Write(ChatSystemPacket.Opcode); w.Write((byte)2); PacketFieldCodec.Unicode(w, "");
                w.Write((uint)(body.Length / 2)); w.Write(body);
            })));
        }
        [Test]
        public async Task TracksServerConfirmedMusicAndDanceSeparatelyAndCountsBothFlourishPacketForms()
        {
            using var assets = new TreRuntimeAssets(ClientSettings.From(Path.Combine(Application.dataPath, "../client.ini")));
            var state = GameServicesForTests.CreateState(out var player); player.BeginScene(11);
            var self = new CreatureSceneObject { SwgId = 11, ListenSwgId = 43, MoodAnimation = "entertained", GroupSwgId = 99 };
            var dancer = new CreatureSceneObject { SwgId = 42, PerformanceAnimation = "dance_1", PerformanceType = 1, GroupSwgId = 99 };
            var musician = new CreatureSceneObject { SwgId = 43, PerformanceAnimation = "music_2", PerformanceType = 50, GroupSwgId = 99 };
            state.SceneObjects.Add(self); state.SceneObjects.Add(dancer); state.SceneObjects.Add(musician);
            using var commands = new ClientCommands(_ => { }, _ => { }, player);
            using var chat = new ClientChat(null, state, new StringTableLocalizer(assets), new ProseFormatter(), commands, player);
            using var combat = new ClientCombat(assets, state);
            using var tracker = new ClientEntertainment(player, state.SceneObjects, chat, combat, assets);
            await tracker.Loading;
            tracker.Refresh(Time.realtimeSinceStartupAsDouble);
            chat.Receive(Watch("dance_watch_self")); await Task.Delay(50);
            tracker.Refresh(Time.realtimeSinceStartupAsDouble);
            Assert.That(tracker.Music.Performer, Is.SameAs(musician));
            Assert.That(tracker.Dance.Performer, Is.SameAs(dancer));
            Assert.That(tracker.Music.StrengthPerPulse, Is.EqualTo(7)); // Native performance row 50, shared with Core3.
            ObjectControllerPacket Controller(uint type, ulong id, Action<BinaryWriter> payload) => ObjectControllerPacket.ReadFrom(new ByteReader(Bytes(w =>
            { w.Write(ObjectControllerPacket.OperandCount); w.Write(ObjectControllerPacket.Opcode); w.Write(0x1bu); w.Write(type); w.Write(id); w.Write(0u); payload(w); })));
            combat.Receive(FlourishPacket.ReadFrom(Controller(FlourishPacket.ControllerType, 43, w => w.Write(3))));
            combat.Receive(AnimationPacket.ReadFrom(Controller(AnimationPacket.ControllerType, 42, w => PacketFieldCodec.Ascii(w, "skill_action_3"))));
            await Task.Delay(50);
            Assert.That(tracker.Music.Estimate.Flourishes, Is.EqualTo(1));
            Assert.That(tracker.Dance.Estimate.Flourishes, Is.EqualTo(1));
            chat.Receive(Watch("music_listen_self", 43)); await Task.Delay(50);
            Assert.That(tracker.Music.Estimate.Flourishes, Is.Zero, "A new confirmed session must reset even with the same listen ID.");
            // A stop packet affects the dance session without stopping concurrent listening.
            chat.Receive(Watch("dance_watch_stop_self")); await Task.Delay(50);
            Assert.That(tracker.Dance, Is.Null);
            self.ListenSwgId = default;
            tracker.Refresh(Time.realtimeSinceStartupAsDouble);
            Assert.That(tracker.Music, Is.Null);
            self.ListenSwgId = 43;
            tracker.Refresh(Time.realtimeSinceStartupAsDouble);
            Assert.That(tracker.Music.Estimate.Flourishes, Is.Zero);
            musician.Skills.Add("social_musician_novice");
            self.ListenSwgId = default; tracker.Refresh(Time.realtimeSinceStartupAsDouble);
            self.ListenSwgId = 43; tracker.Refresh(Time.realtimeSinceStartupAsDouble);
            Assert.That(tracker.Music, Is.Null, "Do not label a known non-master with master estimates.");
        }
        [Test]
        public void HudUsesTheExistingBuffAnchorAndPrefabOwnedRows()
        {
            var root = PrefabUtility.LoadPrefabContents("Assets/Prefabs/UI/Hud/BuffHud.prefab");
            try
            {
                var hud = root.GetComponent<EntertainerProgressHud>();
                Assert.That(hud.buffPanel.gameObject, Is.SameAs(root.GetComponent<BuffHud>().panel));
                Assert.That(hud.music.panel.GetComponent<UnityEngine.UI.VerticalLayoutGroup>(), Is.Not.Null);
                Assert.That(hud.panel.Find("Frame").GetComponent<UnityEngine.UI.LayoutElement>().ignoreLayout, Is.True);
                Assert.That(hud.music.duration.sprite, Is.Not.Null, "Unity Image ignores fillAmount without a sprite.");
                Assert.That(hud.music.duration.fillMethod, Is.EqualTo(UnityEngine.UI.Image.FillMethod.Horizontal));
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}


