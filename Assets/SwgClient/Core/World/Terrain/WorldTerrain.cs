using UnityEngine;
using System;
using System.Linq;
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
        sealed class SamplingCache
        {
            public readonly Dictionary<(int Stage, float X, float Z), float> Heights = new Dictionary<(int, float, float), float>();
            public readonly Dictionary<(int Stage, float X, float Z), float> Normals = new Dictionary<(int, float, float), float>();
        }

        // Each synchronous Sample owns its scratch data. Worker and Unity-thread queries never share it.
        [ThreadStatic]
        static SamplingCache sampling;
        int nextHeightStage;
        int nextLayerIndex;
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
            return result;
        }

        public float SampleHeight(float x, float z) => SampleMaps(x, z, surfaceOnly: true).Height;
        public TerrainSample Sample(float serverX, float serverZ) => SampleMaps(serverX, serverZ, surfaceOnly: false);
        TerrainSample SampleMaps(float serverX, float serverZ, bool surfaceOnly)
        {
            sampling ??= new SamplingCache();
            sampling.Heights.Clear();
            sampling.Normals.Clear();
            return SampleBeforeLayer(serverX, serverZ, int.MaxValue, surfaceOnly);
        }

        TerrainSample SampleBeforeLayer(float x, float z, int endLayer, bool surfaceOnly = false)
        {
            var sample = TerrainSample.Empty;
            foreach (var layer in layers)
            {
                if (layer.Index >= endLayer)
                    break;
                layer.Apply(x, z, 1, ref sample, endLayer, surfaceOnly);
            }

            return sample;
        }

        float SampleNormalYBeforeLayer(float x, float z, int endLayer, int heightStage)
        {
            // Reconstruct the six incident triangle normals from the height map at this stage.
            // Replaying only preceding layers avoids using later city/road heights or recursing into this filter.
            var normalKey = (heightStage, x, z);
            if (sampling.Normals.TryGetValue(normalKey, out var normalY))
                return normalY;
            float step = NativeSpacing;
            float Height(float dx, float dz)
            {
                var key = (heightStage, x + dx * step, z + dz * step);
                if (!sampling.Heights.TryGetValue(key, out var height))
                    sampling.Heights[key] = height = SampleBeforeLayer(key.Item2, key.Item3, endLayer, surfaceOnly: true).Height;
                return height;
            }

            float west = Height(-1, 0);
            float east = Height(1, 0);
            float north = Height(0, 1);
            float south = Height(0, -1);
            float northwest = Height(-1, 1);
            float southeast = Height(1, -1);
            normalY = new Vector3(2 * west + northwest + south - 2 * east - southeast - north, 6 * step, 2 * south + southeast + west - 2 * north - east - northwest).normalized.y;
            sampling.Normals[normalKey] = normalY;
            return normalY;
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
                Index = nextLayerIndex++,
                Enabled = definition.Enabled,
                InvertBoundaries = definition.InvertBoundaries,
                InvertFilters = definition.InvertFilters
            };
            layer.Boundaries.AddRange(definition.Boundaries);
            layer.Filters.AddRange(definition.Filters);
            int heightStage = nextHeightStage;
            layer.SampleNormalY = (x, z) => SampleNormalYBeforeLayer(x, z, layer.Index, heightStage);
            foreach (var affector in definition.Affectors)
            {
                var apply = affectors.Create(affector);
                layer.Affectors.Add(apply);
                if (affector is TrnHeightRule or TrnTerraceRule or TrnRoadRule or TrnShaderRule)
                    layer.SurfaceAffectors.Add(apply);
                if (affector is TrnHeightRule or TrnTerraceRule or TrnRoadRule)
                    nextHeightStage++;
            }

            foreach (var child in definition.Children)
                layer.Children.Add(CreateLayer(child));
            layer.HasSurfaceChanges = layer.SurfaceAffectors.Count > 0 || layer.Children.Any(child => child.HasSurfaceChanges);
            foreach (var kind in definition.UnsupportedRules)
                Debug.LogWarning("Terrain rule not evaluated: " + kind);
            return layer;
        }

        internal NativeTerrainFractal GetFractal(int id) => fractals.TryGetValue(id, out var fractal) ? fractal : throw new InvalidDataException("Missing terrain fractal " + id);
    }
}
