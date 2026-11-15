using System;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Collections.Generic;
using Swg.Client.Core.Networking.Soe;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using Swg.Client.Core;
using Swg.Client.Core.Assets;
using Swg.Client.Core.Assets.Tre;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Assets.Formats.Animation;
using Swg.Client.Core.Characters.Animation;
using Swg.Client.Core.Configuration;
using Swg.Client.Core.Networking.Game.Handlers;
using Swg.Client.Core.Networking.Game.Handlers.Combat;
using Swg.Client.Core.Networking.Game.Packets;
using Swg.Client.Core.Networking.Game.Packets.Combat;
using Swg.Client.Core.Networking.Game.Replication;
using Swg.Client.Core.World;
using Swg.Client.Core.World.SceneObjects;
using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;
using Swg.Client.Runtime.Assets.Appearances;
using Swg.Client.Runtime.Assets.Tre;
using Swg.Client.Runtime.Networking;

namespace Swg.Client.Tests.Editor
{
    public sealed class CombatIntegrationChecks
    {
        static ClientSettings Settings() => ClientSettings.From(Path.Combine(Application.dataPath, "../client.ini"));
        static ObjectControllerPacket ActionPacket(uint crc, byte attackerPosture = 0, byte defenderPosture = 0)
        {
            using var bytes = new MemoryStream();
            using var writer = new BinaryWriter(bytes);
            writer.Write(ObjectControllerPacket.OperandCount); writer.Write(ObjectControllerPacket.Opcode);
            writer.Write(0x1bu); writer.Write(CombatActionPacket.ControllerType); writer.Write(42ul); writer.Write(0u);
            writer.Write(crc); writer.Write(42ul); writer.Write(0ul); writer.Write(attackerPosture); writer.Write((byte)0); writer.Write((byte)0);
            writer.Write((ushort)1); writer.Write(99ul); writer.Write(defenderPosture); writer.Write((byte)1); writer.Write((byte)0);
            return ObjectControllerPacket.ReadFrom(new ByteReader(bytes.ToArray()));
        }
        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        public void StandaloneCombatMessagesArriveWithoutAdvancingTheReliableChannel(bool applicationBundle, bool transportBundle)
        {
            using var peer = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
            var socket = new UdpClient();
            socket.Connect((IPEndPoint)peer.Client.LocalEndPoint);
            using var transport = (SoeTransport)Activator.CreateInstance(typeof(SoeTransport), BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { socket, 123u }, null);
            using var bytes = new MemoryStream();
            using var writer = new BinaryWriter(bytes);
            ActionPacket(ObjectTemplateNames.TemplateCrc("attack_mid_center_light_0")).WriteTo(writer);
            byte[] application = bytes.ToArray();
            byte[] plain = application;
            if (applicationBundle || transportBundle)
                plain = new byte[] { 0, applicationBundle ? (byte)25 : (byte)3, (byte)application.Length }.Concat(application).ToArray();
            var messages = new List<byte[]>();
            var handle = typeof(SoeTransport).GetMethod("Handle", BindingFlags.Instance | BindingFlags.NonPublic);
            handle.Invoke(transport, new object[] { SoeCodec.Decode(SoeCodec.Encode(plain, 123), 123), messages, 0 });
            Assert.That(messages.Single(), Is.EqualTo(application));
            Assert.That(peer.Available, Is.Zero, "Standalone messages do not generate reliable acknowledgements.");
            handle.Invoke(transport, new object[] { SoeCodec.Data(0, application), messages, 0 });
            Assert.That(messages.Count, Is.EqualTo(2), "The first reliable message must still use sequence zero.");
        }

        [TestCase("")]
        [TestCase("sword_1h")]
        [TestCase("sword_2h")]
        [TestCase("polearm")]
        public void NativeBasicAttackVariantsResolveForMeleeWeapons(string weapon)
        {
            var settings = Settings();
            using var loader = new TreAssetLoader(settings.TreDirectory, settings.TreArchivePriorities);
            var table = CbtmFile.Parse(loader.Read("combat/combat_manager.iff"));
            var skeleton = SkeletonData.Load(loader, "appearance/hum_f.sat");
            Assert.That(table.Actions.Count, Is.GreaterThan(400));
            foreach (string height in new[] { "high", "mid", "low" })
            foreach (string direction in new[] { "left", "center", "right" })
            for (int variant = 0; variant < 4; variant++)
            {
                uint crc = ObjectTemplateNames.TemplateCrc("attack_" + height + "_" + direction + "_light_" + variant);
                Assert.That(table.TryGet(crc, 1, 0, 0, 0, out var playback), Is.True);
                Assert.That(skeleton.ResolveAction(playback.Attacker, weapon, CreaturePosture.Upright), Is.Not.Null, playback.Attacker + " / " + weapon);
                Assert.That(skeleton.ResolveAction(playback.Defender, weapon, CreaturePosture.Upright), Is.Not.Null);
            }
        }
        [TestCase("pistol")]
        [TestCase("rifle")]
        public void NativeRangedActionsResolveForStandingKneelingAndProne(string weapon)
        {
            var settings = Settings();
            using var loader = new TreAssetLoader(settings.TreDirectory, settings.TreArchivePriorities);
            var skeleton = SkeletonData.Load(loader, "appearance/hum_f.sat");
            foreach (var posture in new[] { CreaturePosture.Upright, CreaturePosture.Crouched, CreaturePosture.Prone })
                Assert.That(skeleton.ResolveAction("fire_1", weapon, posture), Is.Not.Null, weapon + " " + posture);
        }
        sealed class OwnerContext : SynchronizationContext
        {
            readonly Queue<Action> pending = new Queue<Action>();
            public override void Post(SendOrPostCallback callback, object value) => pending.Enqueue(() => callback(value));
            public void Drain() { while (pending.Count > 0) pending.Dequeue()(); }
        }
        [Test]
        public async Task CombatPosturesUseReplicationOrderWhilePlaybackIsDelayed()
        {
            using var assets = new TreRuntimeAssets(Settings());
            var state = GameServicesForTests.CreateState(out var player);
            var attacker = new CreatureSceneObject { SwgId = 42 };
            var defender = new CreatureSceneObject { SwgId = 99 };
            state.SceneObjects.Add(attacker);
            state.SceneObjects.Add(defender);
            var replication = GameServicesForTests.CreateReplication(state, player);
            var owner = new OwnerContext();
            var previous = SynchronizationContext.Current;
            ClientCombat combat;
            SynchronizationContext.SetSynchronizationContext(owner);
            try { combat = new ClientCombat(assets, state); }
            finally { SynchronizationContext.SetSynchronizationContext(previous); }
            using (combat)
            try
            {
                await combat.Loading;
                int notifications = 0;
                state.SceneObjects.Subscribe(42, _ => notifications++);
                IObjectControllerHandler handler = new CombatActionControllerHandler(combat, replication);
                handler.Handle(ActionPacket(ObjectTemplateNames.TemplateCrc("attack_mid_center_light_0"), (byte)CreaturePosture.Crouched));
                Assert.That(attacker.Posture, Is.EqualTo(CreaturePosture.Upright), "The network handler must queue, not mutate state directly.");
                replication.ApplyPending();
                Assert.That(attacker.Posture, Is.EqualTo(CreaturePosture.Crouched));
                replication.Commit(42, new CreatureSceneObjectUpdate { Posture = (byte)CreaturePosture.Dead });
                replication.ApplyPending();
                owner.Drain(); // Animation work arrives after a newer authoritative posture.
                Assert.That(attacker.Posture, Is.EqualTo(CreaturePosture.Dead));
                Assert.That(notifications, Is.EqualTo(2), "Playback must not publish another stale posture change.");
                Assert.That(attacker.IsInCombat, Is.False);
                Assert.That(defender.Ham.Health.Current, Is.Zero);
            }
            finally { UnityEngine.Object.DestroyImmediate(replication.gameObject); }
        }
        [Test]
        public void CombatPosturesDoNotCreateOrRefineNonCreatureDefenders()
        {
            var state = GameServicesForTests.CreateState(out var player);
            var building = new TangibleSceneObject { SwgId = 99 };
            state.SceneObjects.Add(building);
            var replication = GameServicesForTests.CreateReplication(state, player);
            try
            {
                replication.UpdateCombatPostures(CombatActionPacket.ReadFrom(ActionPacket(1)));
                replication.ApplyPending();
                Assert.That(state.SceneObjects.TryGet(42, out _), Is.False);
                Assert.That(state.SceneObjects.TryGet(99, out var actual), Is.True);
                Assert.That(actual, Is.SameAs(building));
            }
            finally { UnityEngine.Object.DestroyImmediate(replication.gameObject); }
        }
        [Test]
        public async Task CombatPacketsRouteActionsOnTheOwnerThreadWithoutInventingDamageOrCombatState()
        {
            using var assets = new TreRuntimeAssets(Settings());
            var state = GameServicesForTests.CreateState(out var player);
            var attacker = new CreatureSceneObject { SwgId = 42 };
            var defender = new CreatureSceneObject { SwgId = 99 };
            state.SceneObjects.Add(attacker); state.SceneObjects.Add(defender);
            using var combat = new ClientCombat(assets, state);
            await combat.Loading;
            int owner = Thread.CurrentThread.ManagedThreadId;
            string attack = null;
            string reaction = null;
            combat.Subscribe(42, (name, weapon, react) => { Assert.That(Thread.CurrentThread.ManagedThreadId, Is.EqualTo(owner)); attack = name; });
            combat.Subscribe(99, (name, weapon, react) => { Assert.That(react, Is.True); reaction = name; });
            combat.Receive(CombatActionPacket.ReadFrom(ActionPacket(ObjectTemplateNames.TemplateCrc("attack_mid_center_light_0"))));
            await Task.Delay(100);
            Assert.That(attack, Is.EqualTo("attack_mid_center_0"));
            Assert.That(reaction, Is.EqualTo("get_hit_light_mid_center"));
            Assert.That(attacker.IsInCombat, Is.False, "Only replicated state changes enter combat, never an optimistic button press.");
            Assert.That(defender.Ham.Health.Current, Is.Zero, "Damage arrives through HAM replication, not the visual action packet.");
        }
        static async Task Run(IEnumerator routine)
        {
            while (routine.MoveNext())
            {
                if (routine.Current is CustomYieldInstruction wait)
                    while (wait.keepWaiting) await Task.Delay(10);
            }
        }
        [Test]
        public async Task AttackOneShotAnimatesBonesAndReturnsToIdle()
        {
            using var assets = new TreRuntimeAssets(Settings());
            await assets.Ready;
            var skeleton = SkeletonData.Load(assets.Loader, "appearance/hum_f.sat");
            Assert.That(skeleton.ResolveAction("dodge", "", CreaturePosture.Upright), Is.Not.Null);
            var root = new GameObject("Combat animation test");
            try
            {
                var animation = root.AddComponent<CharacterAnimation>();
                var bones = animation.GetBones(skeleton);
                var before = bones.Select(bone => bone.localRotation).ToArray();
                await Run(animation.LoadAction(assets, "attack_mid_center_0", ""));
                animation.Advance(0, .3f);
                Assert.That(animation.IsPlayingAction, Is.True);
                Assert.That(bones.Select((bone, i) => Quaternion.Angle(before[i], bone.localRotation)).Max(), Is.GreaterThan(10));
                animation.Advance(0, 10);
                Assert.That(animation.IsPlayingAction, Is.False);
                animation.SetPosture(CreaturePosture.Dead, true);
                animation.Advance(0, 1);
                int head = Array.FindIndex(skeleton.Joints, j => j.Name.Equals("head", StringComparison.OrdinalIgnoreCase));
                Assert.That(bones[head].position.y, Is.LessThan(1), "Defeated creatures must not stand and idle.");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        [TestCase(CreaturePosture.Dead)]
        [TestCase(CreaturePosture.Incapacitated)]
        [TestCase(CreaturePosture.KnockedDown)]
        [TestCase(CreaturePosture.Crouched)]
        public async Task PostureChangeDiscardsPendingActionAndStanceLoads(CreaturePosture posture)
        {
            using var assets = new TreRuntimeAssets(Settings());
            await assets.Ready;
            var skeleton = SkeletonData.Load(assets.Loader, "appearance/hum_f.sat");
            var root = new GameObject("Pending combat animation test");
            try
            {
                var animation = root.AddComponent<CharacterAnimation>();
                var bones = animation.GetBones(skeleton);
                var action = animation.LoadAction(assets, "attack_mid_center_0", "");
                var stance = animation.LoadStance(assets, "");
                Assert.That(action.MoveNext(), Is.True);
                Assert.That(stance.MoveNext(), Is.True);
                animation.SetPosture(posture, true);
                foreach (var load in new[] { action, stance })
                {
                    var wait = (CustomYieldInstruction)load.Current;
                    while (wait.keepWaiting) await Task.Delay(10);
                    await Run(load);
                }
                Assert.That(animation.IsPlayingAction, Is.False);
                Assert.That(animation.LastAction, Is.Null);
                animation.Advance(0, 1);
                if (posture != CreaturePosture.Crouched)
                {
                    int head = Array.FindIndex(skeleton.Joints, j => j.Name.Equals("head", StringComparison.OrdinalIgnoreCase));
                    Assert.That(bones[head].position.y, Is.LessThan(1), "Pending stance must not replace the fallen pose.");
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        [Test]
        public void EquippedWeaponDeltaNotifiesAnimationStateWithoutRebuildingAppearance()
        {
            var update = new CreatureSceneObjectUpdate();
            CreatureView6.ReadDeltaField(5, new ByteReader(BitConverter.GetBytes(123ul)), update);
            var creature = new CreatureSceneObject(); creature.Apply(update);
            Assert.That(creature.WeaponSwgId, Is.EqualTo(new SwgId(123)));
            Assert.That(update.Changes, Is.EqualTo(SceneObjectChange.State));
        }
    }
}
