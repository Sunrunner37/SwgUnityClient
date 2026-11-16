using Swg.Client.Runtime.World.SceneObjects;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Swg.Client.Runtime.UI.Screens;
using NUnit.Framework;
using Swg.Client.Core.Assets.Appearances;
using Swg.Client.Core.Assets.Formats.Animation;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Assets.Formats.Customization;
using Swg.Client.Core.Assets.Tre;
using Swg.Client.Core.Characters.Animation;
using Swg.Client.Core.Characters.Appearance;
using Swg.Client.Core.Geometry;
using Swg.Client.Core.Networking.Game.Replication;
using Swg.Client.Core.Networking;
using Swg.Client.Core.World;
using Swg.Client.Runtime.Assets.Appearances;
using Swg.Client.Runtime.Assets.Shaders;
using UnityEngine;
using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;

namespace Swg.Client.Tests.Editor
{
    public sealed class RunningChecks
    {
        [Test]
        public void FinalStopWaitsForTheNextMovementSendSlotAndStaysPending()
        {
            var root = new GameObject("Movement send timing check");
            try
            {
                var movement = root.AddComponent<PlayerMovement>();
                var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                void Set(string name, object value) => typeof(PlayerMovement).GetField(name, flags).SetValue(movement, value);
                object Get(string name) => typeof(PlayerMovement).GetField(name, flags).GetValue(movement);
                Set("player", new CreatureSceneObject { RunSpeed = 0 });
                Set("wasMoving", true);
                Set("nextSend", Time.unscaledTime + 1);
                // No connection is supplied: trying to send the stop immediately would fail this check.
                Assert.DoesNotThrow(() => movement.Move(Vector2.zero, .016f));
                Assert.That(Get("pendingMovement"), Is.True);
                Assert.That(Get("wasMoving"), Is.False);
                Assert.DoesNotThrow(() => movement.Move(Vector2.zero, .016f));
                Assert.That(Get("pendingMovement"), Is.True, "The following idle frame must retain the unsent stop.");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [Test]
        public void CharacterPreviewUsesSharedSkinningAndBreathesWithoutMoving()
        {
            using var loader = new TreAssetLoader(@"D:\SwgEmu\emu\SWGEmu");
            var source = SkeletalAppearanceLoader.LoadDetailed(loader, "object/creature/player/shared_human_female.iff");
            var model = new SkeletalAppearanceData();
            foreach (var part in source.Parts)
                model.Parts.Add(new MeshPart { Vertices = part.Vertices, Normals = part.Normals, Uvs = part.Uvs, Triangles = part.Triangles, Skinning = part.Skinning, UseCharacterShader = true, AlphaCutout = part.AlphaCutout });
            var root = new GameObject("preview-test");
            var cameraRoot = new GameObject("preview-camera-test");
            var baked = new Mesh();
            try
            {
                var preview = root.AddComponent<CharacterPreview>();
                preview.previewCamera = cameraRoot.AddComponent<Camera>();
                preview.characterShader = Shader.Find("SWG/Character");
                typeof(CharacterPreview).GetField("materials", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(preview, new SwgMaterialFactory(null));
                const string key = "breathing-preview-test";
                preview.Show(key, model);
                var animation = root.GetComponentInChildren<CharacterAnimation>();
                var renderers = root.GetComponentsInChildren<SkinnedMeshRenderer>();
                Assert.That(renderers.Length, Is.EqualTo(model.Parts.Count));
                Assert.That(root.GetComponentsInChildren<MeshFilter>(), Is.Empty);
                Assert.That(renderers.All(renderer => renderer.gameObject.layer == 31), Is.True);
                Assert.That(Resources.FindObjectsOfTypeAll<Mesh>().Count(mesh => mesh.name.StartsWith(key + " part ")), Is.EqualTo(model.Parts.Count), "Only one GPU mesh per part is needed");
                renderers[0].BakeMesh(baked);
                var before = baked.vertices;
                animation.Advance(0, 1);
                renderers[0].BakeMesh(baked);
                Assert.That(baked.vertices.Select((vertex, index) => Vector3.Distance(vertex, before[index])).Max(), Is.GreaterThan(.00001f));
                Assert.That(animation.Speed, Is.Zero);
                Assert.That(animation.LocomotionBlend, Is.Zero);
                preview.Show(key, model);
                Assert.That(root.GetComponentsInChildren<CharacterAnimation>().Single(), Is.SameAs(animation));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(cameraRoot);
                UnityEngine.Object.DestroyImmediate(baked);
            }
        }

        [Test]
        public void CreatureMovementBaselineMatchesCore3AndUsesItsSpeedLimit()
        {
            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, System.Text.Encoding.ASCII, true))
            {
                writer.Write(1f);
                writer.Write(1f);
                writer.Write(3u);
                writer.Write(1u);
                writer.Write(1u);
                writer.Write(2u);
                writer.Write(3u);
                writer.Write(1u);
                writer.Write(2u);
                writer.Write((byte)0);
                Wire.Ascii(writer, "terrain_negotiation");
                writer.Write(15);
                writer.Write(0);
                writer.Write(1f);
                writer.Write(1.2f);
                writer.Write(0UL);
                writer.Write(7.3f);
                writer.Write(1f);
                writer.Write(0f);
                writer.Write(.75f);
                writer.Write(2.5f);
                writer.Write(1f);
                writer.Write(0u);
                writer.Write(0u);
            }

            var reader = new ByteReader(stream.ToArray());
            var update = CreatureView4.ReadBaseline(reader);
            Assert.That(reader.Remaining, Is.Zero);
            var creature = new CreatureSceneObject();
            creature.Apply(update);
            Assert.That(creature.MovementSpeed, Is.EqualTo(8.76f).Within(.00001f));
            Assert.That(creature.TurnScale, Is.EqualTo(.75f));
        }

        [Test]
        public void MovementDeltasChangeSpeedWithoutRebuildingTheAppearance()
        {
            var creature = new CreatureSceneObject
            {
                RunSpeed = 7.3f
            };
            var update = new CreatureSceneObjectUpdate();
            Assert.That(CreatureView4.ReadDeltaField(5, new ByteReader(BitConverter.GetBytes(.5f)), update), Is.True);
            Assert.That(CreatureView4.ReadDeltaField(7, new ByteReader(BitConverter.GetBytes(8f)), update), Is.True);
            Assert.That(CreatureView4.ReadDeltaField(10, new ByteReader(BitConverter.GetBytes(.8f)), update), Is.True);
            creature.Apply(update);
            Assert.That(creature.MovementSpeed, Is.EqualTo(4));
            Assert.That(creature.TurnScale, Is.EqualTo(.8f));
            Assert.That(update.Changes, Is.EqualTo(SceneObjectChange.None));
        }

        [Test]
        public void NativeRunningTracksAnimateInterpolateAndLoopAtTheirAuthoredRate()
        {
            using var loader = new TreAssetLoader(@"D:\SwgEmu\emu\SWGEmu");
            var animation = CompressedAnimationReader.Read(loader.Read("appearance/animation/all_b_loc_run.ans"));
            Assert.That(animation.FrameRate, Is.EqualTo(30));
            Assert.That(animation.Duration, Is.EqualTo(22f / 30).Within(.00001f));
            Assert.That(animation.LocomotionSpeed, Is.InRange(5f, 5.3f));
            var skeleton = SkeletonData.Load(loader, "appearance/hum_f.sat");
            Assert.That(skeleton.Joints.Length, Is.GreaterThan(50));
            Assert.That(skeleton.RunAnimationPath, Is.EqualTo("appearance/animation/all_b_loc_run.ans"));
            float change = skeleton.Joints.Max(j => Quaternion.Angle(animation.Sample(j.Name, 0).Rotation, animation.Sample(j.Name, animation.Duration * .25f).Rotation));
            Assert.That(change, Is.GreaterThan(10));
            foreach (var joint in skeleton.Joints)
            {
                var first = animation.Sample(joint.Name, .123f);
                var loop = animation.Sample(joint.Name, .123f + animation.Duration);
                Assert.That(Quaternion.Angle(first.Rotation, loop.Rotation), Is.LessThan(.1f));
                Assert.That(Vector3.Distance(first.Translation, loop.Translation), Is.LessThan(.00001f));
            }
        }

        [Test]
        public void GpuSkinMatchesTheNeutralPreviewAndKeepsTheRunCycleThroughTurns()
        {
            using var loader = new TreAssetLoader(@"D:\SwgEmu\emu\SWGEmu");
            var model = SkeletalAppearanceLoader.LoadDetailed(loader, new CharacterAppearance { TemplatePath = "object/creature/player/shared_human_female.iff", Customization = PackedCustomization.Decode(Array.Empty<byte>()), Wearables = new[] { new WearableAppearance { TemplatePath = "object/tangible/hair/human/shared_hair_human_female_s31.iff", Customization = PackedCustomization.Decode(Array.Empty<byte>()) } } });
            var appearance = new AppearanceData
            {
                Key = "running-test"
            };
            foreach (var source in model.Parts)
                appearance.Parts.Add(new MeshPart { Vertices = source.Vertices, Normals = source.Normals, Uvs = source.Uvs, Triangles = source.Triangles, Skinning = source.Skinning });
            using var resources = new AppearanceResources(appearance, new SwgMaterialFactory(null), Shader.Find("SWG/World"), Shader.Find("SWG/Character"), Shader.Find("SWG/Particle"));
            var root = resources.Create(null);
            var baked = new Mesh();
            try
            {
                var renderers = root.GetComponentsInChildren<SkinnedMeshRenderer>();
                Assert.That(renderers.Length, Is.EqualTo(model.Parts.Count));
                for (int part = 0; part < renderers.Length; part++)
                {
                    renderers[part].BakeMesh(baked);
                    float error = baked.vertices.Select((v, i) => Vector3.Distance(v, model.Parts[part].Vertices[i])).Max();
                    Assert.That(error, Is.LessThan(.0001f), "Body, head, and hair must retain the exact neutral pose");
                }

                var animation = root.GetComponent<CharacterAnimation>();
                animation.Advance(7.3f, .2f);
                Assert.That(animation.LocomotionBlend, Is.EqualTo(1));
                float time = animation.LocomotionTime;
                renderers[0].BakeMesh(baked);
                Assert.That(baked.vertices.Select((v, i) => Vector3.Distance(v, model.Parts[0].Vertices[i])).Max(), Is.GreaterThan(.05f));
                root.transform.rotation = Quaternion.Euler(0, 90, 0);
                animation.Advance(7.3f, .2f);
                Assert.That(animation.LocomotionTime, Is.GreaterThan(time), "Turning must not restart the run cycle");
                Assert.That(animation.LocomotionBlend, Is.EqualTo(1));
                animation.Advance(0, .2f);
                Assert.That(animation.LocomotionBlend, Is.Zero);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(baked);
            }
        }
    }
}

