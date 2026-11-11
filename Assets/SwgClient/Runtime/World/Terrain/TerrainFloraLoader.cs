using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Swg.Client.Core.Assets.Formats.Iff;
using Swg.Client.Core.Assets.Formats.Sht;
using Swg.Client.Core.Assets.Formats.Dds;
using Swg.Client.Core.Assets.Appearances;
using Swg.Client.Core.Assets.Tre;
using Swg.Client.Core.Geometry;
using Swg.Client.Core.World.Terrain;
using UnityEngine;

namespace Swg.Client.Runtime.World.Terrain
{
    /// <summary>Builds streamed flora placements and decodes radial grass geometry on the asset worker.</summary>
    public sealed class TerrainFloraLoader
    {
        readonly TreAssetLoader loader;
        readonly WorldTerrain terrain;
        readonly AppearanceCache cache;
        readonly Func<string, AppearanceData> loadAppearance;
        public TerrainFloraLoader(TreAssetLoader loader, WorldTerrain terrain, AppearanceCache cache, Func<string, AppearanceData> loadAppearance)
        {
            this.loader = loader;
            this.terrain = terrain;
            this.cache = cache;
            this.loadAppearance = loadAppearance;
        }

        public TerrainFloraTile LoadTile(Vector2Int key, float tileSize, (float Minimum, float Maximum)[] ranges, CancellationToken token)
        {
            var tile = new TerrainFloraTile();
            var heights = new Dictionary<(int X, int Z), float>();
            var floraMap = new Dictionary<(int X, int Z), TerrainSample>();
            float poleSpacing = terrain.NativeSpacing;
            float Height(int x, int z)
            {
                if (!heights.TryGetValue((x, z), out var height))
                    heights[(x, z)] = height = terrain.SampleHeight(x * poleSpacing, z * poleSpacing);
                return height;
            }

            foreach (TerrainFloraKind kind in Enum.GetValues(typeof(TerrainFloraKind)))
            {
                var distribution = terrain.FloraDistributions[(int)kind];
                var range = ranges[(int)kind];
                float tileMinimumX = key.x * tileSize;
                float tileMinimumZ = key.y * tileSize;
                if (distribution.Spacing <= 0 || range.Maximum <= range.Minimum)
                    continue;
                float spacing = distribution.Spacing;
                int minimumX = (int)Math.Floor(tileMinimumX / spacing);
                int maximumX = (int)Math.Ceiling((tileMinimumX + tileSize) / spacing);
                int minimumZ = (int)Math.Floor(tileMinimumZ / spacing);
                int maximumZ = (int)Math.Ceiling((tileMinimumZ + tileSize) / spacing);
                bool dynamic = kind == TerrainFloraKind.DynamicNear || kind == TerrainFloraKind.DynamicFar;
                for (int z = minimumZ; z < maximumZ; z++)
                    for (int x = minimumX; x < maximumX; x++)
                    {
                        token.ThrowIfCancellationRequested();
                        var point = distribution.PositionAt(x, z);
                        if (point.x < tileMinimumX || point.x >= tileMinimumX + tileSize || point.y < tileMinimumZ || point.y >= tileMinimumZ + tileSize)
                            continue;
                        // Native Chunk::_findMapXz truncates within the chunk: use the lower terrain pole.
                        var pole = ((int)Math.Floor(point.x / poleSpacing), (int)Math.Floor(point.y / poleSpacing));
                        if (!floraMap.TryGetValue(pole, out var sample))
                            floraMap[pole] = sample = terrain.Sample(pole.Item1 * poleSpacing, pole.Item2 * poleSpacing);
                        if (sample.Excluded)
                            continue;
                        var selection = kind switch
                        {
                            TerrainFloraKind.StaticCollidable => sample.StaticCollidable,
                            TerrainFloraKind.StaticNonCollidable => sample.StaticNonCollidable,
                            TerrainFloraKind.DynamicNear => sample.DynamicNear,
                            _ => sample.DynamicFar
                        };
                        var item = terrain.Flora.Select(selection, dynamic);
                        if (item == null)
                            continue;
                        if (!tile.Appearances.TryGetValue(item.Path, out var appearance))
                        {
                            appearance = item.Radial ? LoadRadialAppearance(item) : loadAppearance(item.Path);
                            tile.Appearances.Add(item.Path, appearance);
                        }

                        if (appearance.Parts.Count == 0 && appearance.Particles.Count == 0)
                            continue;
                        var random = new TerrainRandom(point.x, point.y);
                        float height = item.Floats && terrain.HasGlobalWater ? terrain.GlobalWaterHeight : terrain.SampleHeight(point.x, point.y);
                        float scale = item.MinimumScale + (item.MaximumScale - item.MinimumScale) * random.Next();
                        var normal = Vector3.up;
                        if (item.AlignToTerrain)
                        {
                            int poleX = (int)Math.Round(point.x / poleSpacing);
                            int poleZ = (int)Math.Round(point.y / poleSpacing);
                            normal = new Vector3(Height(poleX - 1, poleZ) - Height(poleX + 1, poleZ), poleSpacing * 2, Height(poleX, poleZ - 1) - Height(poleX, poleZ + 1)).normalized;
                        }

                        var size = item.Radial ? item.RadialSize(random.Next(), random.Next()) : Vector3.one;
                        // Near grass uses the terrain color map; far radial silhouettes stay untinted.
                        var color = kind == TerrainFloraKind.DynamicNear ? (Color)sample.Color : Color.white;
                        float yaw = (point.x + point.y) * Mathf.Rad2Deg;
                        tile.Placements.Add(new TerrainFloraPlacement(item, kind, new Vector3(point.x, height, point.y), normal, yaw, scale, size, color));
                    }
            }

            return tile;
        }

        AppearanceData LoadRadialAppearance(TerrainFloraItem item)
        {
            string key = "radial:" + item.Path;
            if (cache.TryGet(key, out var cached))
                return cached;
            var shader = IffNode.Parse(loader.Read(item.Path));
            if (!ShtFile.TryGetMainTexture(shader, out var texturePath))
                throw new InvalidDataException("Radial shader has no MAIN texture: " + item.Path);
            var texture = TextureData.Load(loader, texturePath);
            var part = new MeshPart
            {
                Vertices = new[]
                {
                    new Vector3(-.5f, 0, 0),
                    new Vector3(.5f, 0, 0),
                    new Vector3(-.5f, 1, 0),
                    new Vector3(.5f, 1, 0)
                },
                Normals = new[]
                {
                    Vector3.up,
                    Vector3.up,
                    Vector3.up,
                    Vector3.up
                },
                Uvs = new[]
                {
                    new Vector2(0, 1),
                    new Vector2(1, 1),
                    new Vector2(0, 0),
                    new Vector2(1, 0)
                },
                Triangles = new[]
                {
                    0,
                    2,
                    1,
                    1,
                    2,
                    3
                },
                Diffuse = texture,
                AlphaCutout = true
            };
            var result = new AppearanceData
            {
                Key = key,
                Parts = new List<MeshPart>
                {
                    part
                }
            };
            result.RecalculateBounds();
            cache.Add(key, result);
            return result;
        }
    }
}
