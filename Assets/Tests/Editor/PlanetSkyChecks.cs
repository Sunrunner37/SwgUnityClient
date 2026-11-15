using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using Swg.Client.Core.Assets.Tre;
using Swg.Client.Core.Assets.Appearances;
using Swg.Client.Core.Assets.Formats.DataTable;
using Swg.Client.Core.Assets.Formats.Trn;
using Swg.Client.Core.Configuration;
using Swg.Client.Core.World.Terrain;
using Swg.Client.Runtime.Assets.Particles;
using Swg.Client.Runtime.Assets.Appearances;
using Swg.Client.Runtime.Assets.Tre;
using Swg.Client.Runtime.Assets.Shaders;
using Swg.Client.Runtime.World;
using Object = UnityEngine.Object;

namespace Swg.Client.Tests.Editor
{
    public sealed class PlanetSkyChecks
    {
        static ClientSettings Settings() => ClientSettings.From(Path.Combine(Directory.GetParent(Application.dataPath).FullName, "client.ini"));

        [Test]
        public void NativePlanetEnvironmentTablesLoadTheirOwnGradientsAndBothCloudLayers()
        {
            var settings = Settings();
            using var loader = new TreAssetLoader(settings.TreDirectory, settings.TreArchivePriorities);
            var planets = new List<string>();
            foreach (string path in loader.Catalog().Keys.Where(path => path.StartsWith("datatables/environment/") && path.EndsWith(".iff")))
            {
                var table = DataTableFile.Parse(loader.Read(path));
                if (!table.Columns.Any(column => column.Name == "Gradient Sky Texture (256x256 dds)")) continue;
                var rows = DataTableReader.Read<TerrainEnvironmentCatalog.Row>(table);
                var definitions = rows.Where(row => row.Weather == 0).Select((row, index) => new TrnEnvironmentFamily { Id = index + 1, Name = row.Name }).ToArray();
                var catalog = new TerrainEnvironmentCatalog(loader, definitions, "terrain/" + Path.GetFileNameWithoutExtension(path) + ".trn");
                foreach (var definition in definitions)
                {
                    var row = rows.First(row => row.Weather == 0 && row.Name == definition.Name);
                    var data = catalog.Get(definition.Id);
                    Assert.That(data, Is.Not.Null, path + ":" + row.Name);
                    if (!string.IsNullOrEmpty(row.SkyGradient)) Assert.That(data.SkyGradient, Is.Not.Null, path);
                    Assert.That(data.BottomClouds != null, Is.EqualTo(!string.IsNullOrEmpty(row.BottomShader)), path);
                    Assert.That(data.TopClouds != null, Is.EqualTo(!string.IsNullOrEmpty(row.TopShader)), path);
                    if (data.TopClouds != null)
                    {
                        Assert.That(data.TopClouds.Size, Is.EqualTo(row.TopSize));
                        Assert.That(data.TopClouds.Speed, Is.EqualTo(row.TopSpeed));
                    }
                }
                planets.Add(Path.GetFileNameWithoutExtension(path));
            }
            Assert.That(planets, Does.Contain("corellia"));
            Assert.That(planets, Does.Contain("tatooine"));
            Assert.That(planets, Does.Contain("dathomir"));
            Assert.That(planets.Count, Is.GreaterThanOrEqualTo(10));
        }

        [Test]
        public void WorldPrefabIncludesThePlanetSkyShaderInTheBuild()
        {
            var world = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/World.prefab");
            var shader = (Shader)new SerializedObject(world.GetComponent<WorldEnvironment>()).FindProperty("skyShader").objectReferenceValue;
            Assert.That(shader, Is.Not.Null);
            Assert.That(shader.name, Is.EqualTo("SWG/PlanetSky"));
            Assert.That(ShaderUtil.ShaderHasError(shader), Is.False);
        }

        [Test]
        public void NativeAmbientShipEmittersKeepIndependentStableRandomHeadings()
        {
            using var assets = new TreRuntimeAssets(Settings());
            assets.Ready.GetAwaiter().GetResult();
            var appearance = AppearanceLoader.LoadDetailed(assets.Loader, "appearance/pt_distant_ships.prt");
            using var resources = new AppearanceResources(appearance, new SwgMaterialFactory(assets), Shader.Find("SWG/World"), Shader.Find("SWG/Character"), Shader.Find("SWG/Particle"));
            var root = resources.Create(null);
            try
            {
                var emitters = root.GetComponentsInChildren<ParticleEmitterPlayback>();
                Assert.That(emitters.Length, Is.GreaterThan(2));
                var update = typeof(ParticleEmitterPlayback).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
                var headings = new List<float>();
                foreach (var emitter in emitters)
                {
                    update.Invoke(emitter, null);
                    var rotation = emitter.transform.localRotation;
                    update.Invoke(emitter, null);
                    Assert.That(Quaternion.Angle(rotation, emitter.transform.localRotation), Is.LessThan(.001), "Random headings must not jitter every frame.");
                    headings.Add(emitter.transform.localEulerAngles.y);
                }
                Assert.That(headings.Max() - headings.Min(), Is.GreaterThan(90));
                Assert.That(ParticleEmitterPlayback.MotionSample(123, 4, 0), Is.EqualTo(ParticleEmitterPlayback.MotionSample(123, 4, 0)));
                Assert.That(ParticleEmitterPlayback.MotionSample(123, 4, 1), Is.Not.EqualTo(ParticleEmitterPlayback.MotionSample(123, 4, 0)));
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
