using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Swg.Client.Core.Assets.Appearances;
using Swg.Client.Core.Assets.Formats.Customization;
using Swg.Client.Core.Assets.Tre;
using Swg.Client.Core.Characters.Appearance;
using Swg.Client.Core.Configuration;
using Swg.Client.Core.World;
using Swg.Client.Core.World.SceneObjects;
using Swg.Client.Runtime.Assets.Appearances;
using Swg.Client.Runtime.Assets.Shaders;
using Swg.Client.Runtime.Assets.Tre;
using Swg.Client.Runtime.World.SceneObjects;
using Object = UnityEngine.Object;

namespace Swg.Client.Tests.Editor
{
    public sealed class AppearanceCorrectionChecks
    {
        static ClientSettings Settings() => ClientSettings.From(Path.Combine(Directory.GetParent(Application.dataPath).FullName, "client.ini"));

        [TestCase("object/tangible/wearables/pants/shared_pants_s01.iff", false, true)]
        [TestCase("object/tangible/wearables/skirt/shared_skirt_s04.iff", false, true)]
        [TestCase("object/tangible/wearables/dress/shared_dress_s05.iff", false, false)]
        [TestCase("object/tangible/wearables/shirt/shared_shirt_s03.iff", true, false)]
        [TestCase("object/tangible/wearables/armor/composite/shared_armor_composite_leggings.iff", false, true)]
        [TestCase("object/tangible/wearables/armor/composite/shared_armor_composite_chest_plate.iff", true, false)]
        public void NativeClothingSlotsSuppressOnlyCoveredModestyMeshes(string clothing, bool bottom, bool top)
        {
            var settings = Settings();
            using var loader = new TreAssetLoader(settings.TreDirectory, settings.TreArchivePriorities);
            var appearance = new CharacterAppearance
            {
                TemplatePath = "object/creature/player/shared_human_female.iff",
                Customization = PackedCustomization.Decode(Array.Empty<byte>()),
                Wearables = new[] { new WearableAppearance { TemplatePath = clothing, Customization = PackedCustomization.Decode(Array.Empty<byte>()) } }
            };
            var model = SkeletalAppearanceLoader.LoadDetailed(loader, appearance);
            Assert.That(model.Sources.Any(source => source.MeshPath.Contains("underwear_bottom")), Is.EqualTo(bottom));
            Assert.That(model.Sources.Any(source => source.MeshPath.Contains("underwear_top")), Is.EqualTo(top));
            Assert.That(model.Sources.Any(source => !source.MeshPath.Contains("underwear") && !source.MeshPath.Contains("naked")), Is.True);
        }

        [TestCase("object/tangible/crafting/station/shared_weapon_station.iff", 1f / 6f)]
        [TestCase("object/tangible/crafting/station/shared_structure_station.iff", .1f)]
        [TestCase("object/tangible/crafting/station/shared_food_station.iff", .1f)]
        public void CraftingStationScreensRetainAllNativeFramesAndEmission(string path, float interval)
        {
            var settings = Settings();
            using var loader = new TreAssetLoader(settings.TreDirectory, settings.TreArchivePriorities);
            var appearance = AppearanceLoader.LoadDetailed(loader, path);
            var screen = appearance.Parts.Single(part => part.DiffuseFrames != null);
            Assert.That(screen.DiffuseFrames.Length, Is.EqualTo(8));
            Assert.That(screen.MinimumFrameSeconds, Is.EqualTo(interval).Within(.0001));
            Assert.That(screen.MaximumFrameSeconds, Is.EqualTo(screen.MinimumFrameSeconds));
            Assert.That(screen.Fullbright, Is.True);
            Assert.That(screen.DiffuseFrames.Skip(1).Any(frame => !frame.Pixels.SequenceEqual(screen.Diffuse.Pixels)), Is.True);
            Assert.That(appearance.Warnings, Is.Empty);
        }

        [Test]
        public void UploadedStationAnimatesWithoutModifyingSharedMaterialsOrOtherProperties()
        {
            var settings = Settings();
            using var assets = new TreRuntimeAssets(settings);
            assets.Ready.GetAwaiter().GetResult();
            var appearance = AppearanceLoader.LoadDetailed(assets.Loader, "object/tangible/crafting/station/shared_weapon_station.iff");
            using var resources = new AppearanceResources(appearance, new SwgMaterialFactory(assets), Shader.Find("SWG/World"), Shader.Find("SWG/Character"), Shader.Find("SWG/Particle"));
            var root = resources.Create(null);
            try
            {
                var animation = root.GetComponentInChildren<AnimatedTexture>();
                Assert.That(animation, Is.Not.Null);
                var renderer = animation.GetComponent<Renderer>();
                Assert.That(renderer.sharedMaterial.GetFloat("_Fullbright"), Is.EqualTo(1));
                var properties = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(properties);
                var first = properties.GetTexture("_BaseMap");
                properties.SetFloat("_SwgPreview", 1);
                renderer.SetPropertyBlock(properties);
                var play = (System.Collections.IEnumerator)typeof(AnimatedTexture).GetMethod("Play", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(animation, null);
                play.MoveNext();
                play.MoveNext();
                renderer.GetPropertyBlock(properties);
                Assert.That(properties.GetTexture("_BaseMap"), Is.Not.SameAs(first));
                Assert.That(properties.GetFloat("_SwgPreview"), Is.EqualTo(1));
                Assert.That(renderer.sharedMaterial.GetTexture("_BaseMap"), Is.SameAs(first));
                var hologram = root.GetComponentsInChildren<Renderer>().Single(item => item.sharedMaterial.GetFloat("_AlphaBlend") == 1);
                Assert.That(hologram.sharedMaterial.renderQueue, Is.EqualTo(3000));
                Assert.That(hologram.sharedMaterial.GetFloat("_ZWrite"), Is.Zero);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void RoomContentsAreNonBlockingAndRemainPickableAcrossContainmentChanges()
        {
            var registry = new SceneObjectRegistry();
            registry.Add(new CellSceneObject { SwgId = 10 });
            var prop = new TangibleSceneObject { SwgId = 20 };
            registry.Add(prop);
            var instance = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(instance.GetComponent<BoxCollider>());
            var pick = instance.AddComponent<BoxCollider>();
            pick.isTrigger = true;
            try
            {
                WorldSceneObjectRenderer.ConfigureCollision(prop, instance);
                var meshCollider = instance.GetComponent<MeshCollider>();
                Assert.That(meshCollider.enabled, Is.True);
                registry.Reparent(20, 10);
                WorldSceneObjectRenderer.ConfigureCollision(prop, instance);
                Assert.That(meshCollider.enabled, Is.False);
                Assert.That(pick.enabled && pick.isTrigger, Is.True);
                registry.Reparent(20, default);
                WorldSceneObjectRenderer.ConfigureCollision(prop, instance);
                Assert.That(meshCollider.enabled, Is.True);
            }
            finally { Object.DestroyImmediate(instance); }
        }
    }
}
