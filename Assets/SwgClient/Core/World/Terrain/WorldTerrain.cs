using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Swg.Client.Core.Assets.Formats.Trn;
using Swg.Client.Core.Assets.Tre;

namespace Swg.Client.Core.World.Terrain
{
    /// <summary>Evaluates native PTAT terrain layers directly from the mounted TRE archives.</summary>
    public sealed class WorldTerrain
    {
        readonly Dictionary<int, NativeTerrainFractal> fractals = new Dictionary<int, NativeTerrainFractal>();
        readonly List<TerrainLayer> layers = new List<TerrainLayer>();
        readonly Dictionary<int, TerrainSurface> surfaces = new Dictionary<int, TerrainSurface>();
        readonly Dictionary<int, TrnShaderFamily> surfaceDefinitions = new Dictionary<int, TrnShaderFamily>();
        TreAssetLoader assets;
        TerrainAffectorFactory affectors;
        public TerrainFloraDistribution[] FloraDistributions { get; private set; }
        public float NativeSpacing { get; private set; }
        public TerrainFloraCatalog Flora { get; private set; }
        public TerrainEnvironmentCatalog Environments { get; private set; }
        public float Size { get; private set; }
        public float ChunkSize { get; private set; }
        public bool HasGlobalWater { get; private set; }
        public float GlobalWaterHeight { get; private set; }
        public string Path { get; private set; }

        public static WorldTerrain Load(TreAssetLoader loader, string terrainPath)
        {
            var result = new WorldTerrain
            {
                Path = terrainPath,
                assets = loader
            };
            var definition = TrnFile.Parse(loader.Read(terrainPath));
            result.Size = definition.Size;
            result.ChunkSize = definition.ChunkSize;
            result.NativeSpacing = definition.ChunkSize / definition.TilesPerChunk * .5f;
            result.FloraDistributions = definition.FloraDistributions;
            result.HasGlobalWater = definition.HasGlobalWater;
            result.GlobalWaterHeight = definition.GlobalWaterHeight;
            foreach (var fractal in definition.Fractals)
                result.fractals[fractal.Key] = fractal.Value;
            foreach (var family in definition.Shaders)
                result.surfaceDefinitions[family.Id] = family;
            result.Flora = new TerrainFloraCatalog(definition.Flora);
            result.Environments = new TerrainEnvironmentCatalog(loader, definition.Environments, terrainPath);
            result.affectors = new TerrainAffectorFactory(result, loader);
            foreach (var layer in definition.Layers)
                result.layers.Add(result.CreateLayer(layer));
            Debug.LogWarning("Slope/shader filters still use the Core3 height-evaluation fallback.");
            return result;
        }

        public float SampleHeight(float x, float z) => Sample(x, z).Height;
        public TerrainSample Sample(float serverX, float serverZ)
        {
            var sample = TerrainSample.Empty;
            foreach (var layer in layers)
                layer.Apply(serverX, serverZ, 1, ref sample);
            return sample;
        }

        internal TrnShaderFamily GetSurfaceDefinition(int id) => surfaceDefinitions.TryGetValue(id, out var family) ? family : null;
        public TerrainSurface GetSurface(int familyId)
        {
            if (surfaces.TryGetValue(familyId, out var surface))
                return surface;
            if (!surfaceDefinitions.TryGetValue(familyId, out var family))
                return null;
            surface = TerrainSurface.Load(assets, family);
            surfaces.Add(familyId, surface);
            return surface;
        }

        TerrainLayer CreateLayer(TrnLayer definition)
        {
            var layer = new TerrainLayer
            {
                Enabled = definition.Enabled,
                InvertBoundaries = definition.InvertBoundaries,
                InvertFilters = definition.InvertFilters
            };
            layer.Boundaries.AddRange(definition.Boundaries);
            layer.Filters.AddRange(definition.Filters);
            foreach (var affector in definition.Affectors)
                layer.Affectors.Add(affectors.Create(affector));
            foreach (var child in definition.Children)
                layer.Children.Add(CreateLayer(child));
            foreach (var kind in definition.UnsupportedRules)
                Debug.LogWarning("Terrain rule not evaluated: " + kind);
            return layer;
        }

        internal NativeTerrainFractal GetFractal(int id) => fractals.TryGetValue(id, out var fractal) ? fractal : throw new InvalidDataException("Missing terrain fractal " + id);
    }
}
