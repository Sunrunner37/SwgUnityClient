using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Swg.Client.Core.Assets.Tre;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Assets.Formats.Iff;
using Swg.Client.Core.Assets.Formats.Animation;
using Swg.Client.Core.Characters.Animation;
using Swg.Client.Core.Configuration;
using Swg.Client.Core.Networking.Game.Handlers.Movement;
using Swg.Client.Core.Networking.Game.Handlers;
using Swg.Client.Core.Networking.Game.Packets.Movement;
using Swg.Client.Core.Networking.Game.Replication;
using Swg.Client.Core.World;
using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;
using Swg.Client.Runtime;
using Swg.Client.Runtime.Assets.Appearances;
using Swg.Client.Runtime.World.SceneObjects;
using UnityEngine;

namespace Swg.Client.Tests.Editor
{
    public sealed class CreatureMovementChecks
    {
        static TreAssetLoader Assets()
        {
            var settings = ClientSettings.From(Path.Combine(Application.dataPath, "../client.ini"));
            return new TreAssetLoader(settings.TreDirectory, settings.TreArchivePriorities);
        }

        [TestCase("appearance/drall_m.sat", "drall", "walk", "run")]
        [TestCase("appearance/gubbur.sat", "frog", "walk", "run")]
        [TestCase("appearance/corellian_butterfly.sat", "insect_moth", "fly_slow", "fly_fast")]
        public void SpeciesUseTheirOwnIdleAndSpeedMappedLocomotion(string path, string species, string slow, string fast)
        {
            using var assets = Assets();
            var skeleton = SkeletonData.Load(assets, path);
            Assert.That(skeleton.IdleAnimationPath, Does.Contain(species + "_idl_"));
            Assert.That(skeleton.WalkAnimationPath, Does.EndWith("_loc_" + slow + ".ans"));
            Assert.That(skeleton.RunAnimationPath, Does.EndWith("_loc_" + fast + ".ans"));
            Assert.That(skeleton.Walk.LocomotionSpeed, Is.GreaterThan(0));
            Assert.That(skeleton.Run.LocomotionSpeed, Is.GreaterThan(skeleton.Walk.LocomotionSpeed));
            var root = new GameObject("Creature locomotion");
            try
            {
                var animation = root.AddComponent<CharacterAnimation>();
                var bones = animation.GetBones(skeleton);
                animation.SetPosture(CreaturePosture.Upright, true);
                animation.Advance(skeleton.Walk.LocomotionSpeed, .2f);
                Assert.That(animation.WalkRunBlend, Is.Zero);
                var before = bones.Select(bone => bone.localRotation).ToArray();
                animation.Advance(skeleton.Walk.LocomotionSpeed, skeleton.Walk.Duration * .25f);
                Assert.That(bones.Select((bone, i) => Quaternion.Angle(before[i], bone.localRotation)).Max(), Is.GreaterThan(1));
                float phase = animation.LocomotionTime;
                root.transform.rotation = Quaternion.Euler(0, 90, 0);
                animation.Advance(skeleton.Run.LocomotionSpeed, .2f);
                Assert.That(animation.WalkRunBlend, Is.EqualTo(1).Within(.0001f));
                Assert.That(animation.LocomotionTime, Is.GreaterThan(phase), "Turning and gait changes must not restart the cycle.");
                animation.Advance(0, .2f);
                Assert.That(animation.LocomotionBlend, Is.Zero);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [Test]
        public void NativeMovementChannelsExcludeTurnAndMoodAnimations()
        {
            using var assets = Assets();
            var table = LatFile.Parse(assets.Read("appearance/lat/drall.lat")).Animations["loop_standing"];
            Assert.That(table.LocomotionClips, Is.EquivalentTo(new[] { "appearance/animation/drall_loc_walk.ans", "appearance/animation/drall_loc_run.ans" }));
            Assert.That(table.IdleClips, Is.EquivalentTo(new[] { "appearance/animation/drall_idl_stand_breathe.ans" }));
        }

        [Test]
        public void RemoteMovementSpansTheServerTravelTimeAndSleepsAtRest()
        {
            var registry = new SceneObjectRegistry();
            using var services = ManagedObjectTestScope.Use(registry);
            var creature = new CreatureSceneObject { SwgId = 42 };
            registry.Add(creature);
            var root = new GameObject("Remote creature interpolation");
            try
            {
                var movement = root.AddComponent<SceneObjectMovement>();
                movement.Initialize(creature);
                void Receive(float x, float speed)
                {
                    creature.Apply(new MovementUpdate(new Vector3(x, 0, 0), Quaternion.Euler(0, 90, 0), speed: speed));
                    registry.NotifyChanged(creature, SceneObjectChange.Transform);
                }
                Receive(.8f, 2);
                movement.Advance(.1f);
                Assert.That(root.transform.position.x, Is.EqualTo(.2f).Within(.001f), "A 400 ms segment must not finish in 100 ms.");
                movement.Advance(.3f);
                Assert.That(root.transform.position.x, Is.EqualTo(.8f).Within(.001f));
                Receive(1.6f, 2);
                movement.Advance(.2f);
                Assert.That(root.transform.position.x, Is.EqualTo(1.2f).Within(.001f));
                Receive(2.4f, 2); // Early/jittered arrival starts at the current visual pose.
                Assert.That(root.transform.position.x, Is.EqualTo(1.2f).Within(.001f));
                movement.Advance(.2f);
                Assert.That(root.transform.position.x, Is.EqualTo(1.6f).Within(.001f));
                Receive(2, 0);
                movement.Advance(.2f);
                Assert.That(root.transform.position.x, Is.EqualTo(2).Within(.001f));
                Assert.That(movement.enabled, Is.False);
                Assert.That(creature.Position.x, Is.EqualTo(2), "Visual interpolation never changes authoritative coordinates.");
                Receive(100, 2);
                Assert.That(root.transform.position.x, Is.EqualTo(100), "Teleports still snap.");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void BroadcastAndControllerTransformsRetainServerTravelSpeed(bool withParent)
        {
            var state = GameServicesForTests.CreateState(out var player);
            var replication = GameServicesForTests.CreateReplication(state, player);
            try
            {
                state.SceneObjects.Add(new CreatureSceneObject { SwgId = 42 });
                using var packetBytes = new MemoryStream();
                using var packetWriter = new BinaryWriter(packetBytes);
                IGamePacketHandler handler;
                if (withParent)
                {
                    new UpdateTransformWithParentPacket(42, Vector3.right, 1, 2, 25, 0).WriteTo(packetWriter);
                    handler = new UpdateTransformWithParentPacketHandler(replication);
                }
                else
                {
                    new UpdateTransformPacket(42, Vector3.right, 1, 2, 25).WriteTo(packetWriter);
                    handler = new UpdateTransformPacketHandler(replication);
                }
                handler.Handle(new ByteReader(packetBytes.ToArray()));
                replication.ApplyPending();
                Assert.That(((CreatureSceneObject)state.SceneObjects.Single()).ServerSpeed, Is.EqualTo(2));
                using var bytes = new MemoryStream();
                using var writer = new BinaryWriter(bytes);
                writer.Write(2u);
                if (withParent) writer.Write(0ul);
                writer.Write(0f); writer.Write(0f); writer.Write(0f); writer.Write(1f);
                writer.Write(1f); writer.Write(0f); writer.Write(0f); writer.Write(2.35f);
                var update = MovementReader.ReadControllerTransform(new ByteReader(bytes.ToArray()), withParent);
                Assert.That(update.Speed, Is.EqualTo(2.35f));
            }
            finally { UnityEngine.Object.DestroyImmediate(replication.gameObject); }
        }
    }
}

