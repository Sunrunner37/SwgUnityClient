using Swg.Client.Core.Crafting;
using Swg.Client.Core.Characters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using Swg.Client.Core.Assets.Tre;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Characters.Animation;
using Swg.Client.Core.Configuration;
using Swg.Client.Core.Networking.Game.Handlers;
using Swg.Client.Core.Networking.Game.Handlers.Movement;
using Swg.Client.Core.Networking.Game.Handlers.Effects;
using Swg.Client.Core.Networking.Game.Packets;
using Swg.Client.Core.Networking.Game.Packets.Data;
using Swg.Client.Core.Networking.Game.Packets.Effects;
using Swg.Client.Core.Networking.Game.Packets.Movement;
using Swg.Client.Core.World;
using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;
using Swg.Client.Runtime.Networking;
using Swg.Client.Runtime.Localization;
using Swg.Client.Runtime.Assets.Tre;
using Swg.Client.Runtime.Assets.Appearances;
using UnityEngine;

namespace Swg.Client.Tests.Editor
{
    public sealed class PostureChecks
    {
        sealed class OwnerContext : SynchronizationContext
        {
            readonly Queue<Action> pending = new Queue<Action>();
            public override void Post(SendOrPostCallback callback, object state) => pending.Enqueue(() => callback(state));
            public void Drain() { while (pending.Count > 0) pending.Dequeue()(); }
        }
        static ClientSettings Settings() => ClientSettings.From(Path.Combine(Application.dataPath, "../client.ini"));
        static ObjectControllerPacket Packet(uint type, Action<BinaryWriter> payload)
        {
            using var bytes = new MemoryStream();
            using var writer = new BinaryWriter(bytes);
            writer.Write(ObjectControllerPacket.OperandCount);
            writer.Write(ObjectControllerPacket.Opcode);
            writer.Write(0x1bu);
            writer.Write(type);
            writer.Write(42ul);
            writer.Write(0u);
            payload(writer);
            return ObjectControllerPacket.ReadFrom(new ByteReader(bytes.ToArray()));
        }
        [Test]
        public void PostureMessageUpdatesCreatureOnMainThreadWithoutRefreshingAppearance()
        {
            var root = new GameObject("Posture replication test");
            try
            {
                var registry = new SceneObjectRegistry();
                var creature = new CreatureSceneObject { SwgId = 42 };
                registry.Add(creature);
                var replication = root.AddComponent<GameReplication>();
                replication.Initialize(GameServicesForTests.CreateState(registry, out var currentPlayer), currentPlayer, new CraftingProgress(), _ => { });
                var changes = SceneObjectChange.None;
                registry.Subscribe(42, value => changes |= value);
                IObjectControllerHandler handler = new PostureControllerHandler(replication);
                handler.Handle(Packet(PosturePacket.ControllerType, writer => { writer.Write((byte)8); writer.Write((byte)1); }));
                Assert.That(creature.Posture, Is.EqualTo(CreaturePosture.Upright));
                replication.ApplyPending();
                Assert.That(creature.Posture, Is.EqualTo(CreaturePosture.Sitting));
                Assert.That(changes, Is.EqualTo(SceneObjectChange.Posture));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        [Test]
        public void FlyTextPacketLocalizesAndPreservesObjectColorAndScaleOnOwnerContext()
        {
            using var assets = new TreRuntimeAssets(Settings());
            var localizer = new StringTableLocalizer(assets);
            var owner = new OwnerContext();
            var previous = SynchronizationContext.Current;
            ClientFlyText text;
            SynchronizationContext.SetSynchronizationContext(owner);
            try { text = new ClientFlyText(localizer); }
            finally { SynchronizationContext.SetSynchronizationContext(previous); }
            using (text)
            {
                var messages = new List<ClientFlyText.Message>();
                text.Received += messages.Add;
                IObjectControllerHandler handler = new ShowFlyTextControllerHandler(text);
                handler.Handle(Packet(ShowFlyTextPacket.ControllerType, writer =>
                {
                    writer.Write(42ul);
                    PacketFieldCodec.Ascii(writer, "combat_effects");
                    writer.Write(0u);
                    PacketFieldCodec.Ascii(writer, "go_rooted");
                    writer.Write(1.5f);
                    writer.Write((byte)40); writer.Write((byte)180); writer.Write((byte)255); writer.Write((byte)5);
                }));
                Assert.That(messages, Is.Empty);
                owner.Drain();
                Assert.That(messages.Count, Is.EqualTo(1));
                Assert.That(messages[0].SwgId.Value, Is.EqualTo(42));
                Assert.That(messages[0].Text, Is.EqualTo(localizer.Lookup("combat_effects", "go_rooted")));
                Assert.That(messages[0].Text, Does.Not.StartWith("@"));
                Assert.That(messages[0].Color, Is.EqualTo((Color)new Color32(40, 180, 255, 255)));
                Assert.That(messages[0].Scale, Is.EqualTo(1.5f));
            }
        }
        [Test]
        public void ProneLocomotionUsesNativeCrawlAndKeepsItsPostureWhenMovingOrStopping()
        {
            var settings = Settings();
            using var assets = new TreAssetLoader(settings.TreDirectory, settings.TreArchivePriorities);
            var skeleton = SkeletonData.Load(assets, "appearance/hum_f.sat");
            Assert.That(skeleton.CrawlAnimationPath, Is.EqualTo("appearance/animation/all_b_loc_prone_crawl.ans"));
            Assert.That(skeleton.Crawl.LocomotionSpeed, Is.GreaterThan(0));
            var root = new GameObject("Native crawling test");
            try
            {
                var animation = root.AddComponent<CharacterAnimation>();
                var bones = animation.GetBones(skeleton);
                int head = Array.FindIndex(skeleton.Joints, joint => joint.Name == "Head");
                float standingHeight = bones[head].position.y;
                animation.SetPosture(CreaturePosture.Prone, true);
                var before = bones.Select(bone => bone.localRotation).ToArray();
                animation.Advance(.8f, .3f);
                Assert.That(animation.Posture, Is.EqualTo(CreaturePosture.Prone));
                Assert.That(animation.IsChangingPosture, Is.False);
                Assert.That(animation.LocomotionBlend, Is.EqualTo(1));
                Assert.That(animation.LocomotionTime, Is.EqualTo(.3f * .8f / skeleton.Crawl.LocomotionSpeed).Within(.0001f));
                Assert.That(bones.Select((bone, index) => Quaternion.Angle(bone.localRotation, before[index])).Max(), Is.GreaterThan(1));
                Assert.That(bones[head].position.y, Is.LessThan(standingHeight * .5f));
                float time = animation.LocomotionTime;
                root.transform.rotation = Quaternion.Euler(0, 90, 0);
                animation.Advance(.8f, .3f);
                Assert.That(animation.LocomotionTime, Is.GreaterThan(time), "Turning must not restart crawling");
                animation.Advance(0, .2f);
                Assert.That(animation.Posture, Is.EqualTo(CreaturePosture.Prone));
                Assert.That(animation.LocomotionBlend, Is.Zero);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [TestCase(CreaturePosture.Crouched)]
        [TestCase(CreaturePosture.Prone)]
        [TestCase(CreaturePosture.Sitting)]
        public void NativePostureTransitionsFinishInTheirLoweredIdlePose(CreaturePosture posture)
        {
            var settings = Settings();
            using var assets = new TreAssetLoader(settings.TreDirectory, settings.TreArchivePriorities);
            var skeleton = SkeletonData.Load(assets, "appearance/hum_f.sat");
            var root = new GameObject("Native posture animation test");
            try
            {
                var animation = root.AddComponent<CharacterAnimation>();
                var bones = animation.GetBones(skeleton);
                int head = Array.FindIndex(skeleton.Joints, joint => joint.Name == "Head");
                Assert.That(head, Is.GreaterThanOrEqualTo(0));
                float standingHeight = bones[head].position.y;
                var transition = skeleton.Transitions[(CreaturePosture.Upright, posture)];
                Assert.That(transition.Duration, Is.GreaterThan(0));
                Assert.That(skeleton.Joints.Any(joint => Quaternion.Angle(transition.Sample(joint.Name, 0, false).Rotation,
                    transition.Sample(joint.Name, transition.Duration, false).Rotation) > .1f), Is.True);
                foreach (var joint in skeleton.Joints)
                {
                    var end = transition.Sample(joint.Name, transition.Duration, false);
                    var held = transition.Sample(joint.Name, transition.Duration + 5, false);
                    Assert.That(Vector3.Distance(end.Translation, held.Translation), Is.LessThan(.00001f));
                    Assert.That(Quaternion.Angle(end.Rotation, held.Rotation), Is.LessThan(.01f));
                }
                animation.SetPosture(posture);
                Assert.That(animation.IsChangingPosture, Is.True);
                animation.Advance(0, transition.Duration + 1);
                Assert.That(bones[head].position.y, Is.LessThan(standingHeight * .9f));
                Assert.That(animation.IsChangingPosture, Is.False);
                animation.SetPosture(posture == CreaturePosture.Sitting ? CreaturePosture.Prone : CreaturePosture.Sitting);
                animation.Advance(0, 10);
                Assert.That(bones[head].position.y, Is.LessThan(standingHeight * .9f), "Postures without a direct clip should bridge through standing");
                animation.SetPosture(CreaturePosture.Upright);
                animation.Advance(0, 10);
                Assert.That(bones[head].position.y, Is.EqualTo(standingHeight).Within(.1f));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
    }
}
