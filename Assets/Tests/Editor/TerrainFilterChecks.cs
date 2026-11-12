using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Color32 = UnityEngine.Color32;
using Swg.Client.Core.Assets.Tre;
using NUnit.Framework;
using Swg.Client.Core.Assets.Formats.Trn;
using Swg.Client.Core.World.Terrain;
using Swg.Client.Core.World.Terrain.Filters;

namespace Swg.Client.Tests.Editor
{
    public sealed class TerrainFilterChecks
    {
        static byte[] Bytes(Action<BinaryWriter> write)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            write(writer);
            return stream.ToArray();
        }
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void HeightRangeUsesCurrentHeightAndPreservesFeathering(int featherType)
        {
            var filter = new TrnFilterReader(new Dictionary<int, NativeTerrainFractal>()).Read("FHGT", Bytes(writer =>
            {
                writer.Write(0f); writer.Write(100f); writer.Write(featherType); writer.Write(.4f);
            }));
            Assert.That(filter, Is.TypeOf<TerrainHeightFilter>());
            Assert.That(filter.Sample(1000, -300, new TerrainSample { Height = 0 }), Is.Zero);
            Assert.That(filter.Sample(1000, -300, new TerrainSample { Height = 100 }), Is.Zero);
            Assert.That(filter.Sample(1000, -300, new TerrainSample { Height = 50 }), Is.EqualTo(1));
            float expected = featherType switch
            {
                1 => .25f,
                2 => (float)Math.Sqrt(.5f),
                _ => .5f
            };
            Assert.That(filter.Sample(1000, -300, new TerrainSample { Height = 10 }), Is.EqualTo(expected).Within(.000001f));
            Assert.That(filter.Sample(-800, 900, new TerrainSample { Height = 90 }), Is.EqualTo(expected).Within(.000001f));
        }
        [Test]
        public void FractalFilterDecodesItsOwnLayoutAndSamplesCoordinatesInsteadOfHeight()
        {
            var fractal = new NativeTerrainFractal(TrnFractal.Parse(Bytes(writer =>
            {
                writer.Write(123); writer.Write(0); writer.Write(.5f); writer.Write(0); writer.Write(.5f); writer.Write(3);
                writer.Write(2f); writer.Write(.5f); writer.Write(.1f); writer.Write(.1f); writer.Write(0f); writer.Write(0f); writer.Write(0);
            })));
            var filter = (TerrainFractalFilter)new TrnFilterReader(new Dictionary<int, NativeTerrainFractal> { [7] = fractal }).Read("FFRA", Bytes(writer =>
            {
                writer.Write(7); writer.Write(0); writer.Write(.4f); writer.Write(0f); writer.Write(10f); writer.Write(10f);
            }));
            Assert.That(filter.FractalId, Is.EqualTo(7));
            Assert.That(filter.Multiplier, Is.EqualTo(10));
            for (int z = -5; z <= 5; z++)
                for (int x = -5; x <= 5; x++)
                {
                    float value = fractal.Sample(x, z) * 10;
                    float expected = value <= 0 || value >= 10 ? 0 : Math.Min(1, Math.Min(value, 10 - value) / 2);
                    Assert.That(filter.Sample(x, z, new TerrainSample { Height = -1000 }), Is.EqualTo(expected).Within(.000001f));
                    Assert.That(filter.Sample(x, z, new TerrainSample { Height = 1000 }), Is.EqualTo(expected).Within(.000001f));
                }
        }
        [Test]
        public void FiltersSeePrecedingHeightsAndShaderBeforeLaterFlattening()
        {
            using var assets = new TreAssetLoader(@"D:\SwgEmu\emu\SWGEmu");
            var terrain = WorldTerrain.Load(assets, "terrain/corellia.trn");
            var layers = (List<TerrainLayer>)typeof(WorldTerrain).GetField("layers", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(terrain);
            TerrainLayer FindSlope(IEnumerable<TerrainLayer> candidates)
            {
                foreach (var layer in candidates)
                {
                    if (layer.Filters.Any(filter => filter.NeedsNormals))
                        return layer;
                    var child = FindSlope(layer.Children);
                    if (child != null)
                        return child;
                }
                return null;
            }
            // Reuse a bound native normal sampler in a controlled three-layer fixture.
            var filtered = FindSlope(layers);
            filtered.Index = 1;
            filtered.Enabled = true;
            filtered.InvertBoundaries = false;
            filtered.InvertFilters = false;
            filtered.Boundaries.Clear();
            filtered.Children.Clear();
            filtered.Filters.Clear();
            filtered.Affectors.Clear();
            filtered.SurfaceAffectors.Clear();
            filtered.HasSurfaceChanges = false;
            filtered.Filters.Add(new TerrainShaderFilter(9));
            filtered.Filters.Add(new TerrainSlopeFilter(40, 50, 0, 0));
            filtered.Affectors.Add((float x, float z, float weight, ref TerrainSample sample) => sample.Color = new Color32(139, 105, 101, 255));
            TerrainLayer Surface(int index, TerrainLayer.Affector apply)
            {
                var layer = new TerrainLayer { Index = index, Enabled = true, HasSurfaceChanges = true };
                layer.Affectors.Add(apply);
                layer.SurfaceAffectors.Add(apply);
                return layer;
            }
            layers.Clear();
            layers.Add(Surface(0, (float x, float z, float weight, ref TerrainSample sample) => { sample.Height = x; sample.ShaderFamily = 9; }));
            layers.Add(filtered);
            layers.Add(Surface(2, (float x, float z, float weight, ref TerrainSample sample) => { sample.Height = 0; sample.ShaderFamily = 10; }));
            var result = terrain.Sample(0, 0);
            Assert.That(result.Height, Is.Zero);
            Assert.That(result.ShaderFamily, Is.EqualTo(10));
            Assert.That(result.Color, Is.EqualTo(new Color32(139, 105, 101, 255)));
            Assert.That(terrain.Sample(0, 0).Color, Is.EqualTo(result.Color));
        }

        [Test]
        public void SlopeAndShaderFiltersUseCurrentNormalsAndShaderFamily()
        {
            var slope = (TerrainSlopeFilter)new TrnFilterReader(new Dictionary<int, NativeTerrainFractal>()).Read("FSLP", Bytes(writer =>
            {
                writer.Write(15f); writer.Write(45f); writer.Write(3); writer.Write(.25f);
            }));
            Assert.That(slope.MinimumDegrees, Is.EqualTo(15));
            Assert.That(slope.MaximumDegrees, Is.EqualTo(45));
            Assert.That(slope.FeatherType, Is.EqualTo(3));
            Assert.That(slope.Feather, Is.EqualTo(.25f));
            var shader = (TerrainShaderFilter)new TrnFilterReader(new Dictionary<int, NativeTerrainFractal>()).Read("FSHD", Bytes(writer => writer.Write(9)));
            Assert.That(shader.FamilyId, Is.EqualTo(9));
            Assert.That(slope.Sample(0, 0, TerrainSample.Empty, (float)Math.Cos(Math.PI / 6)), Is.EqualTo(1));
            Assert.That(slope.Sample(0, 0, TerrainSample.Empty, 1), Is.Zero);
            Assert.That(shader.Sample(0, 0, new TerrainSample { ShaderFamily = 9 }), Is.EqualTo(1));
            Assert.That(shader.Sample(0, 0, new TerrainSample { ShaderFamily = 10 }), Is.Zero);
        }
    }
}
