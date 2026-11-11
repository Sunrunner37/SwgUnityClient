using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Swg.Client.Core.Assets.Formats.Dds;
using Swg.Client.Core.World.Terrain;
using Swg.Client.Runtime.World;

namespace Swg.Client.Tests.Editor
{
    public sealed class WorldEnvironmentChecks
    {
        [Test]
        public void NativeRampSamplingPreservesChannelsAndAlphaIntensity()
        {
            var data = new TerrainEnvironmentData { ColorRamp = new TextureData(2, 8) };
            void Row(int row, byte red, byte green, byte blue, byte alpha)
            {
                int offset = row * 2 * 4;
                data.ColorRamp.Pixels[offset] = red;
                data.ColorRamp.Pixels[offset + 1] = green;
                data.ColorRamp.Pixels[offset + 2] = blue;
                data.ColorRamp.Pixels[offset + 3] = alpha;
            }
            Row(0, 10, 20, 30, 255);
            Row(1, 255, 128, 0, 160);
            Row(3, 0, 255, 0, 192);
            Row(4, 0, 0, 255, 160);
            Row(5, 40, 50, 60, 255);
            Row(6, 70, 80, 90, 255);
            var sample = data.Sample(.25f);
            Assert.That(sample.AmbientEquator.r, Is.EqualTo(10f / 255).Within(.00001f));
            Assert.That(sample.AmbientSky.g, Is.EqualTo(20f / 255 + 2).Within(.00001f));
            Assert.That(sample.AmbientGround.b, Is.EqualTo(30f / 255 + 1).Within(.00001f));
            Assert.That(sample.SunIntensity, Is.EqualTo(1).Within(.00001f));
            Assert.That(sample.Clear.r, Is.EqualTo(40f / 255).Within(.00001f));
            Assert.That(sample.Fog.r, Is.EqualTo(70f / 255).Within(.00001f));
            Assert.That(data.GetColumn(0), Is.EqualTo(0));
            Assert.That(data.GetColumn(.5f), Is.EqualTo(1));
            Assert.That(data.GetColumn(1), Is.EqualTo(1));
            Assert.That(data.Sample(.75f).SunIntensity, Is.Zero);
        }

        [Test]
        public void WorldPrefabOwnsAnInitiallyDisabledEnvironmentWithExplicitReferences()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/World.prefab");
            var controller = prefab.GetComponent<WorldController>();
            var environment = prefab.GetComponent<WorldEnvironment>();
            Assert.That(environment, Is.Not.Null);
            Assert.That(controller.environment, Is.SameAs(environment));
            Assert.That(environment.enabled, Is.False);
            var serialized = new SerializedObject(environment);
            var sun = (Light)serialized.FindProperty("sun").objectReferenceValue;
            Assert.That(sun.type, Is.EqualTo(LightType.Directional));
            Assert.That(sun.transform.IsChildOf(prefab.transform), Is.True);
            Assert.That(serialized.FindProperty("cameraView").objectReferenceValue, Is.SameAs(controller.orbit.worldCamera));
        }
    }
}
