using Swg.Client.Runtime.World.Terrain;
using System.Text;
using Swg.Client.Core.Assets.Formats.Iff;
using System.Linq;
using System.Threading;
using Swg.Client.Runtime.Assets.Tre;
using Swg.Client.Core.Configuration;
using Swg.Client.Core.Assets.Formats.Dds;
using Swg.Client.Runtime.Assets.Appearances;
using Swg.Client.Runtime.World.Streaming;
using Swg.Client.Core.Assets.Appearances;
using Swg.Client.Runtime.Assets.Shaders;
using Swg.Client.Runtime.World;
using Swg.Client.Core.Geometry;
using System;
using System.IO;
using NUnit.Framework;
using Swg.Client.Core.Assets.Formats.Trn;
using Swg.Client.Core.Assets.Tre;
using Swg.Client.Core.World.Terrain;
using UnityEngine;

namespace Swg.Client.Tests.Editor
{
    public sealed class NativeFloraChecks
    {
        [Test]
        public void RazorcoilPatchUsesNativeFiltersAndAlphaMaskedThorns()
        {
            using var assets = new TreAssetLoader(@"D:\SwgEmu\emu\SWGEmu");
            var terrain = WorldTerrain.Load(assets, "terrain/corellia.trn");
            var sample = terrain.Sample(-156, -5006);
            var plant = terrain.Flora.Select(sample.StaticNonCollidable, false);
            Assert.That(plant, Is.Not.Null);
            Assert.That(plant.Path, Does.StartWith("appearance/leaf_tals_razorcoil"));
            var appearance = AppearanceLoader.LoadDetailed(assets, plant.Path);
            Assert.That(appearance.Warnings, Is.Empty);
            Assert.That(appearance.Parts.Count, Is.EqualTo(2));
            Assert.That(appearance.Parts.Any(part => part.AlphaCutout), Is.True);
            Assert.That(appearance.Parts.Any(part => !part.AlphaCutout), Is.True, "The opaque spine must remain opaque");
        }

        [Test]
        public void NativeReferencesNormalizeSeparatorsAndCaseBeforeMatching()
        {
            var bytes = Encoding.ASCII.GetBytes("effect\\a_alpha.eft\0EFFECT/A_ALPHA.EFT\0texture/unrelated.dds\0");
            Assert.That(IffNode.References(bytes, "effect/", ".eft"), Is.EqualTo(new[] { "effect/a_alpha.eft" }));
        }

        [Test]
        public void NativePlantLodsUseCheaperMeshesWithoutChangingTheHighDetailDefault()
        {
            using var assets = new TreAssetLoader(@"D:\SwgEmu\emu\SWGEmu");
            var high = AppearanceLoader.LoadDetailed(assets, "appearance/decd_windswept.apt");
            var low = AppearanceLoader.LoadDetailed(assets, "appearance/decd_windswept.apt", AppearanceLoader.LowestDetail);
            Assert.That(high.Sources.Any(source => source.AppearancePath.EndsWith("_l0.msh")), Is.True);
            Assert.That(low.Sources.Any(source => source.AppearancePath.EndsWith("_l3.msh")), Is.True);
            Assert.That(low.Parts.Sum(part => part.Vertices.Length), Is.LessThan(high.Parts.Sum(part => part.Vertices.Length)));
        }

        [Test]
        public void StreamedGrassDetailsAreStableNestedSubsetsOfTheNativePlacements()
        {
            using var assets = new TreAssetLoader(@"D:\SwgEmu\emu\SWGEmu");
            var terrain = WorldTerrain.Load(assets, "terrain/corellia.trn");
            var settings = ScriptableObject.CreateInstance<WorldStreamingSettings>();
            try
            {
                var loader = new TerrainFloraLoader(assets, terrain, new AppearanceCache(), path => AppearanceLoader.LoadDetailed(assets, path));
                var ranges = terrain.FloraDistributions.Select((distribution, kind) => settings.GetFloraRange(distribution, (TerrainFloraKind)kind)).ToArray();
                var key = new Vector2Int(-23, -626);
                var full = loader.LoadTile(key, 8, ranges, CancellationToken.None);
                for (int detail = 1; detail <= 2; detail++)
                {
                    var sparse = loader.LoadTile(key, 8, ranges, CancellationToken.None, detail);
                    var expected = full.Placements.Where(p => p.Kind == TerrainFloraKind.DynamicNear && p.Detail >= detail).Select(p => p.ServerPosition).ToArray();
                    Assert.That(sparse.Placements.Where(p => p.Kind == TerrainFloraKind.DynamicNear).Select(p => p.ServerPosition), Is.EqualTo(expected));
                    Assert.That(expected.Length, Is.LessThan(full.Placements.Count(p => p.Kind == TerrainFloraKind.DynamicNear)));
                }

                foreach (var point in new[]
                {
                    new Vector2(-156, -5006),
                    new Vector2(-183, -5002),
                    new Vector2(300, 400)
                }

                )
                    Assert.That(terrain.SampleHeight(point.x, point.y), Is.EqualTo(terrain.Sample(point.x, point.y).Height).Within(.0001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void RepeatingWorldTexturesHaveMipsWhileUiTexturesKeepTheirOriginalUpload()
        {
            using var assets = new TreRuntimeAssets(ClientSettings.From(Path.Combine(Application.dataPath, "../client.ini")));
            var source = new TextureData(4, 4);
            var world = assets.Texture(source, TextureWrapMode.Repeat);
            var ui = assets.Texture(source, TextureWrapMode.Clamp);
            Assert.That(world.mipmapCount, Is.EqualTo(3));
            Assert.That(world.filterMode, Is.EqualTo(FilterMode.Trilinear));
            Assert.That(ui.mipmapCount, Is.EqualTo(1));
            Assert.That(assets.Texture(source, TextureWrapMode.Repeat), Is.SameAs(world));
        }

        [Test]
        public void NativeCorelliaDefinesOneMeterGrassAndSeparateStaticDistributions()
        {
            using var assets = new TreAssetLoader(@"D:\SwgEmu\emu\SWGEmu");
            var file = TrnFile.Parse(assets.Read("terrain/corellia.trn"));
            var near = file.FloraDistributions[(int)TerrainFloraKind.DynamicNear];
            Assert.That(near.Spacing, Is.EqualTo(1));
            Assert.That(near.Border, Is.EqualTo(.48f));
            Assert.That(near.MaximumDistance, Is.EqualTo(24));
            Assert.That(file.FloraDistributions[(int)TerrainFloraKind.StaticCollidable].Spacing, Is.EqualTo(6));
            Assert.That(file.FloraDistributions[(int)TerrainFloraKind.StaticNonCollidable].Spacing, Is.EqualTo(5));
            Assert.That(file.FloraDistributions[(int)TerrainFloraKind.DynamicFar].MinimumDistance, Is.EqualTo(105));
        }

        [Test]
        public void PlacementUsesNativeBorderAndIsStableAcrossStreaming()
        {
            var distribution = new TerrainFloraDistribution
            {
                Spacing = 5,
                Border = 1,
                Seed = 7
            };
            var point = distribution.PositionAt(-12, 9);
            Assert.That(point, Is.EqualTo(distribution.PositionAt(-12, 9)));
            Assert.That(point.x, Is.InRange(-59f, -56f));
            Assert.That(point.y, Is.InRange(46f, 49f));
            Assert.That(point - new Vector2(-60, 45), Is.Not.EqualTo(distribution.PositionAt(2, 9) - new Vector2(10, 45)));
        }

        [Test]
        public void RadialSizeUsesHalfWidthsAndAuthoredIndependentRanges()
        {
            var item = new TerrainFloraItem
            {
                MinimumWidth = 1,
                MaximumWidth = 3,
                MinimumHeight = 2,
                MaximumHeight = 6
            };
            Assert.That(item.RadialSize(0, 0), Is.EqualTo(new Vector3(2, 2, 2)));
            Assert.That(item.RadialSize(1, 1), Is.EqualTo(new Vector3(6, 6, 6)));
            Assert.That(item.RadialSize(.25f, .75f), Is.EqualTo(new Vector3(3, 5, 3)));
            item.MaintainAspectRatio = true;
            Assert.That(item.RadialSize(.25f, .75f), Is.EqualTo(new Vector3(3, 3, 3)));
            item.MaximumWidth = item.MinimumWidth;
            Assert.That(item.RadialSize(.75f, .75f).y, Is.EqualTo(item.MinimumHeight));
        }

        [Test]
        public void FloraRangesCombineAuthoredDistancesPreferencesAndParticleBudget()
        {
            var settings = ScriptableObject.CreateInstance<WorldStreamingSettings>();
            try
            {
                Assert.That(settings.GetFloraRange(new TerrainFloraDistribution(), TerrainFloraKind.DynamicNear), Is.EqualTo((0f, 0f)));
                var near = new TerrainFloraDistribution
                {
                    MinimumDistance = 0,
                    MaximumDistance = 24
                };
                Assert.That(settings.GetFloraRange(near, TerrainFloraKind.DynamicNear), Is.EqualTo((0f, 320f)));
                var distant = new TerrainFloraDistribution
                {
                    MinimumDistance = 105,
                    MaximumDistance = 650
                };
                Assert.That(settings.GetFloraRange(distant, TerrainFloraKind.DynamicFar), Is.EqualTo((105f, 320f)));
                var scenery = new TerrainFloraDistribution
                {
                    MinimumDistance = 2,
                    MaximumDistance = 200
                };
                Assert.That(settings.GetFloraRange(scenery, TerrainFloraKind.StaticNonCollidable, particles: true), Is.EqualTo((2f, 64f)));
                settings.particleFloraDistance = 20;
                Assert.That(settings.GetFloraRange(near, TerrainFloraKind.DynamicNear, particles: true), Is.EqualTo((0f, 20f)));
                settings.floraRadius = 12;
                Assert.That(settings.GetFloraRange(near, TerrainFloraKind.DynamicNear), Is.EqualTo((0f, 12f)));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void UnifiedAppearanceUploadPreservesMaterialMetadataAndMeshData()
        {
            var source = new MeshPart
            {
                Vertices = new[]
                {
                    Vector3.zero,
                    Vector3.right,
                    Vector3.up
                },
                Uvs = new[]
                {
                    new Vector2(0, .25f),
                    Vector2.right,
                    Vector2.up
                },
                Triangles = new[]
                {
                    0,
                    1,
                    2
                }
            };
            source.AlphaCutout = true;
            source.VertexColors = new byte[]
            {
                255,
                0,
                0,
                255,
                0,
                255,
                0,
                255,
                0,
                0,
                255,
                255
            };
            var appearance = new AppearanceData
            {
                Key = "test"
            };
            appearance.Parts.Add(source);
            var resources = MeshPartResources.FromAppearance(appearance, 0, new SwgMaterialFactory(null), Shader.Find("SWG/World"));
            var material = resources.Material;
            var mesh = resources.Mesh;
            try
            {
                Assert.That(material.GetFloat("_Cutoff"), Is.EqualTo(.35f));
                Assert.That(material.GetFloat("_UseVertexColor"), Is.EqualTo(1));
                Assert.That(mesh.uv[0], Is.EqualTo(new Vector2(0, .75f)));
                Assert.That(mesh.colors32, Is.EqualTo(new[] { new Color32(255, 0, 0, 255), new Color32(0, 255, 0, 255), new Color32(0, 0, 255, 255) }));
                Assert.That(mesh.normals.Length, Is.EqualTo(3));
                Assert.That(Vector3.Dot(mesh.normals[0], Vector3.forward), Is.GreaterThan(.99f));
                Assert.That(mesh.bounds.Contains(new Vector3(.5f, .5f, 0)), Is.True);
            }
            finally
            {
                resources.Dispose();
            }

            Assert.That(mesh == null, Is.True);
            Assert.That(material == null, Is.True);
        }

        [Test]
        public void NativeClearanceIndexMatchesCircleTestsAcrossCellBoundaries()
        {
            var circles = new[]
            {
                new FloraClearance(new Vector3(-32, 10, -32), 8),
                new FloraClearance(new Vector3(20, 0, 20), 40)
            };
            var index = new FloraClearanceIndex(circles);
            for (int z = -80; z <= 80; z++)
                for (int x = -80; x <= 80; x++)
                {
                    var position = new Vector3(x, -999, z);
                    Assert.That(index.Contains(position), Is.EqualTo(circles[0].Contains(position) || circles[1].Contains(position)));
                }
        }

        [Test]
        public void ClientDataDecodesAuthoredClearanceCentersAndRadii()
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            writer.Write(Encoding.ASCII.GetBytes("CNCF"));
            writer.Write(new byte[] { 0, 0, 0, 16 });
            writer.Write(12f);
            writer.Write(3f);
            writer.Write(-5f);
            writer.Write(9f);
            var clearance = FloraClearance.FromClientData(stream.ToArray());
            Assert.That(clearance.Length, Is.EqualTo(1));
            Assert.That(clearance[0].Center, Is.EqualTo(new Vector3(12, 3, -5)));
            Assert.That(clearance[0].Radius, Is.EqualTo(9));
        }

        [Test]
        public void FineTerrainEdgesMatchTheirCoarseNeighbors()
        {
            using var assets = new TreAssetLoader(@"D:\SwgEmu\emu\SWGEmu");
            var terrain = WorldTerrain.Load(assets, "terrain/corellia.trn");
            var fine = WorldTerrainPatch.Build(terrain, new Vector3(-176, 0, -5024), 16, 2, new Vector4(0, 8, 0, 0));
            var coarse = WorldTerrainPatch.Build(terrain, new Vector3(-144, 0, -5024), 16, 8);
            const int fineEdge = 17;
            const int coarseEdge = 5;
            for (int i = 0; i < fineEdge; i++)
            {
                int segment = Math.Min(i / 4, coarseEdge - 2);
                float amount = (i - segment * 4) / 4f;
                var expected = Vector3.Lerp(coarse.Vertices[segment * coarseEdge], coarse.Vertices[(segment + 1) * coarseEdge], amount);
                Assert.That((fine.Vertices[i * fineEdge + fineEdge - 1] - expected).sqrMagnitude, Is.LessThan(.000001f));
                var normal = Vector3.Lerp(coarse.Normals[segment * coarseEdge], coarse.Normals[(segment + 1) * coarseEdge], amount).normalized;
                Assert.That((fine.Normals[i * fineEdge + fineEdge - 1] - normal).sqrMagnitude, Is.LessThan(.000001f));
            }
        }
    }
}
