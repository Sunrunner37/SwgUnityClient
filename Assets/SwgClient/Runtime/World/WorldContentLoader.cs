using Swg.Client.Core.Assets.Formats.Sht;
using Swg.Client.Core.Assets.Formats.Dds;
using Swg.Client.Core.Assets.Formats.Iff;
using Swg.Client.Core.Assets.Formats.Eft;
using Swg.Client.Core.Characters.Materials;
using Swg.Client.Core.Configuration;
using Swg.Client.Runtime.Networking;
using Swg.Client.Core;
using Swg.Client.Runtime.Assets.Tre;
using Swg.Client.Core.World.SceneObjects;
using Swg.Client.Core.Assets.Models;
using Swg.Client.Core.Assets.Formats.Customization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Swg.Client.Core.Assets.Tre;
using Swg.Client.Core.Characters.Appearance;
using Swg.Client.Core.Characters.Models;
using Swg.Client.Core.Geometry;
using Swg.Client.Core.Networking.Game;
using Swg.Client.Core.World;
using Swg.Client.Core.World.Terrain;
using UnityEngine;

namespace Swg.Client.Runtime.World
{
    /// <summary>Shared native asset decoding and per-world model cache, serialized across entry and streaming.</summary>
    public sealed class WorldContentLoader
    {
        readonly TreRuntimeAssets assets;
        readonly WorldState data;
        readonly ModelCache models;
        readonly string cacheDirectory;
        readonly string cacheServer;
        readonly string cacheUsername;
        readonly string cacheGalaxy;
        byte[] lastCachedAppearance;
        readonly SemaphoreSlim reads = new SemaphoreSlim(1, 1);
        public WorldContentLoader(TreRuntimeAssets assets, WorldState data, ModelCache models, ClientSettings settings, ClientConnection connection)
        {
            this.assets = assets;
            this.models = models;
            this.data = data;
            cacheDirectory = settings.AppearanceCacheDirectory;
            cacheServer = settings.LoginEndpoint;
            cacheUsername = connection.Username;
            cacheGalaxy = connection.Admission.Galaxy.Name;
        }

        public async Task OpenAsync(GameWorldSnapshot initial, CancellationToken token)
        {
            var start = initial.StartScene.Value;
            var loaded = await assets.ReadAsync(loader =>
            {
                token.ThrowIfCancellationRequested();
                var templates = new ObjectTemplateCatalog(loader);
                var terrain = WorldTerrain.Load(loader, start.TerrainPath);
                var snapshot = WorldSnapshot.Load(loader, Path.GetFileNameWithoutExtension(start.TerrainPath));
                token.ThrowIfCancellationRequested();
                return (templates, terrain, snapshot);
            });
            token.ThrowIfCancellationRequested();
            data.Templates = loaded.templates;
            data.Terrain = loaded.terrain;
            data.Snapshot = loaded.snapshot;
            foreach (var entry in data.Snapshot.Entries)
                if (!data.SceneObjects.TryGet(entry.SwgId, out _))
                    data.SceneObjects.Add(SceneObjectFactory.Create(entry));
        }

        async Task<T> ReadAsync<T>(Func<TreAssetLoader, T> read, CancellationToken token)
        {
            await reads.WaitAsync(token);
            try
            {
                return await assets.ReadAsync(loader =>
                {
                    token.ThrowIfCancellationRequested();
                    var result = read(loader);
                    token.ThrowIfCancellationRequested();
                    return result;
                });
            }
            finally
            {
                reads.Release();
            }
        }

        public Task<WorldRegionData> LoadRegionAsync(Vector3 serverCenter, WorldStreamingSettings settings, HashSet<Vector2Int> loadedTerrain, HashSet<SwgId> loadedScenery, IProgress<WorldPreparationProgress> progress, CancellationToken token, HashSet<Vector2Int> loadedFlora = null)
        {
            // Copy settings and caller-owned sets before entering the worker.
            float tileSize = settings.terrainTileSize;
            float spacing = settings.terrainSpacing;
            float radius = settings.terrainRadius;
            float viewRadius = settings.viewRadius;
            float floraRadius = settings.floraRadius;
            float floraTileSize = settings.floraTileSize;
            float floraSpacing = settings.floraSpacing;
            var flora = loadedFlora == null ? new HashSet<Vector2Int>() : new HashSet<Vector2Int>(loadedFlora);
            var terrain = new HashSet<Vector2Int>(loadedTerrain);
            var scenery = new HashSet<SwgId>(loadedScenery);
            return ReadAsync(loader =>
            {
                var region = new WorldRegionData
                {
                    ServerCenter = serverCenter
                };
                region.TerrainKeys.UnionWith(WorldRegionPlanner.TerrainKeys(serverCenter, radius, tileSize));
                int count = 0;
                foreach (var key in region.TerrainKeys)
                {
                    token.ThrowIfCancellationRequested();
                    if (!terrain.Contains(key))
                        region.Terrain.Add(key, WorldTerrainPatch.Build(data.Terrain, new Vector3((key.x + .5f) * tileSize, 0, (key.y + .5f) * tileSize), tileSize * .5f, spacing));
                    progress?.Report(new WorldPreparationProgress("Reading original terrain...", .4f * ++count / region.TerrainKeys.Count));
                }

                region.FloraKeys.UnionWith(WorldRegionPlanner.TerrainKeys(serverCenter, floraRadius, floraTileSize));
                foreach (var key in region.FloraKeys)
                {
                    token.ThrowIfCancellationRequested();
                    if (!flora.Contains(key)) region.Flora.Add(key, LoadFloraTile(loader, key, floraTileSize, floraSpacing, token));
                }
                var entries = data.Snapshot.Query(serverCenter, viewRadius, 256).ToArray();
                count = 0;
                foreach (var entry in entries)
                {
                    token.ThrowIfCancellationRequested();
                    if (++count % 100 == 0)
                        progress?.Report(new WorldPreparationProgress("Loading scenery " + count + "/" + entries.Length, .4f + .6f * count / entries.Length));
                    if (WorldAppearance.IsCell(entry.TemplatePath))
                        continue;
                    if (scenery.Contains(entry.SwgId))
                    {
                        region.ScenerySwgIds.Add(entry.SwgId);
                        continue;
                    }

                    var model = LoadModel(loader, new WorldAppearance(entry.TemplatePath));
                    if (model.Parts.Count == 0)
                        continue;
                    region.ScenerySwgIds.Add(entry.SwgId);
                    region.Scenery.Add(entry.SwgId, model);
                }

                progress?.Report(new WorldPreparationProgress("Region assets prepared", 1));
                return region;
            }, token);
        }

        TerrainFloraTile LoadFloraTile(TreAssetLoader loader, Vector2Int key, float tileSize, float spacing, CancellationToken token)
        {
            var tile = new TerrainFloraTile();
            var samples = new Dictionary<(int X, int Z), TerrainSample>();
            TerrainSample At(int x, int z)
            {
                if (!samples.TryGetValue((x, z), out var value))
                {
                    value = data.Terrain.Sample((x + .5f) * spacing, (z + .5f) * spacing);
                    samples.Add((x, z), value);
                }
                return value;
            }
            int minimumX = (int)Math.Ceiling(key.x * tileSize / spacing);
            int maximumX = (int)Math.Ceiling((key.x + 1) * tileSize / spacing);
            int minimumZ = (int)Math.Ceiling(key.y * tileSize / spacing);
            int maximumZ = (int)Math.Ceiling((key.y + 1) * tileSize / spacing);
            for (int z = minimumZ; z < maximumZ; z++)
                for (int x = minimumX; x < maximumX; x++)
                {
                    token.ThrowIfCancellationRequested();
                    float serverX = (x + .5f) * spacing;
                    float serverZ = (z + .5f) * spacing;
                    var sample = At(x, z);
                    var random = new TerrainRandom(serverX, serverZ);
                    void Place(TerrainFloraSelection selection, TerrainFloraKind kind)
                    {
                        bool dynamic = kind == TerrainFloraKind.DynamicNear || kind == TerrainFloraKind.DynamicFar;
                        var item = data.Terrain.Flora.Select(selection, dynamic);
                        if (item == null || (!dynamic && ((x & 1) != 0 || (z & 1) != 0))) return;
                        if (!tile.Models.TryGetValue(item.Path, out var model))
                        {
                            model = item.Radial ? LoadRadialModel(loader, item) : LoadModel(loader, new WorldAppearance(item.Path));
                            tile.Models.Add(item.Path, model);
                        }
                        if (model.Parts.Count == 0) return;
                        float height = item.Floats && data.Terrain.HasGlobalWater ? data.Terrain.GlobalWaterHeight : sample.Height;
                        float scale = item.MinimumScale + (item.MaximumScale - item.MinimumScale) * random.Next();
                        tile.Placements.Add(new TerrainFloraPlacement(item, kind, new Vector3(serverX, height, serverZ),
                            item.AlignToTerrain ? new Vector3(At(x - 1, z).Height - At(x + 1, z).Height, spacing * 2, At(x, z - 1).Height - At(x, z + 1).Height).normalized : Vector3.up,
                            random.Next() * 360, scale));
                    }
                    Place(sample.StaticCollidable, TerrainFloraKind.StaticCollidable);
                    Place(sample.StaticNonCollidable, TerrainFloraKind.StaticNonCollidable);
                    Place(sample.DynamicNear, TerrainFloraKind.DynamicNear);
                    Place(sample.DynamicFar, TerrainFloraKind.DynamicFar);
                }
            return tile;
        }

        ModelData LoadRadialModel(TreAssetLoader loader, TerrainFloraItem item)
        {
            string key = "radial:" + item.Path;
            if (models.TryGet(key, out var cached)) return cached;
            var shader = IffNode.Parse(loader.Read(item.Path));
            if (!ShtFile.TryGetMainTexture(shader, out var texturePath))
                throw new InvalidDataException("Radial shader has no MAIN texture: " + item.Path);
            var texture = TextureData.Load(loader, texturePath);
            var part = new MeshPart
            {
                Vertices = new[] { new Vector3(-.5f, 0, 0), new Vector3(.5f, 0, 0), new Vector3(-.5f, 1, 0), new Vector3(.5f, 1, 0) },
                Normals = new[] { Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward },
                Uvs = new[] { new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 0), new Vector2(1, 0) },
                Triangles = new[] { 0, 2, 1, 1, 2, 3 },
                Diffuse = texture
            };
            var result = new ModelData { Key = key, Parts = new List<MeshPart> { part }, AlphaCutout = new[] { true } };
            models.Add(key, result);
            return result;
        }

        public Task<Dictionary<SwgId, ModelData>> LoadModelsAsync(GameWorldSnapshot snapshot, IReadOnlyDictionary<SwgId, WorldAppearance> appearances, CancellationToken token)
        {
            return ReadAsync(loader =>
            {
                var models = new Dictionary<SwgId, ModelData>();
                foreach (var pair in appearances)
                {
                    token.ThrowIfCancellationRequested();
                    try
                    {
                        var model = LoadModel(loader, pair.Value);
                        if (model.Parts.Count != 0)
                            models.Add(pair.Key, model);
                    }
                    catch (Exception error)
                    {
                        Debug.LogWarning("SceneObject " + pair.Key + ": " + error.Message);
                    }
                }

                CachePlayerAppearance(snapshot);
                return models;
            }, token);
        }

        void CachePlayerAppearance(GameWorldSnapshot snapshot)
        {
            var player = snapshot.Objects.FirstOrDefault(item => item.SwgId == snapshot.PlayerSwgId);
            if (player == null || player is not CreatureSceneObject || !snapshot.PlayerBaselinesComplete)
                return;
            string path = data.Templates.Resolve(player.TemplateCrc) ?? player.TemplatePath;
            if (string.IsNullOrEmpty(path))
                return;
            try
            {
                var appearance = WorldAppearance.Describe(data, snapshot, player)?.Character;
                if (appearance == null)
                    return;
                var bytes = CharacterAppearanceCache.Serialize(appearance);
                if (lastCachedAppearance != null && bytes.SequenceEqual(lastCachedAppearance))
                    return;
                CharacterAppearanceCache.SaveProfile(cacheDirectory, cacheServer, cacheUsername, cacheGalaxy, player.SwgId, appearance);
                lastCachedAppearance = bytes;
            }
            catch (Exception error)when (error is IOException || error is UnauthorizedAccessException || error is InvalidDataException || error is ArgumentException)
            {
                Debug.LogWarning("Character appearance cache could not be saved: " + error.Message);
            }
        }

        ModelData LoadModel(TreAssetLoader loader, WorldAppearance description)
        {
            string path = description.TemplatePath;
            var appearance = description.Character;
            string key = description.Key;
            if (models.TryGet(key, out var cached))
                return cached;
            var model = new ModelData
            {
                Key = key,
                Parts = new List<MeshPart>()
            };
            models.Add(key, model);
            try
            {
                if (appearance == null)
                {
                    var native = StaticModel.LoadDetailed(loader, path);
                    model.Parts = native.Parts;
                    model.AlphaCutout = native.Sources.Select(s => s.AlphaCutoutHint).ToArray();
                    model.VertexColors = native.Sources.Select(s => s.VertexColors).ToArray();
                    foreach (var warning in native.Warnings)
                        Debug.LogWarning(warning);
                    if (model.Parts.Count != 0)
                        return model;
                    if (!path.Contains("/creature/"))
                        return model;
                    appearance = new CachedCharacterAppearance
                    {
                        TemplatePath = path,
                        Customization = PackedCustomization.Decode(Array.Empty<byte>()),
                        Wearables = Array.Empty<CachedWearableAppearance>(),
                        Planet = "",
                        SourcePath = "",
                        FromCache = false
                    };
                }

                var character = CachedCharacterModel.LoadDetailed(loader, appearance);
                model.Parts = character.Parts;
                model.Character = true;
                model.AlphaCutout = character.Sources.Select(s => (s.ObjectTemplate ?? "").Contains("/hair/") || (s.MeshPath ?? "").Contains("/hair/")).ToArray();
                model.VertexColors = new byte[model.Parts.Count][];
                foreach (var warning in character.Warnings)
                    Debug.LogWarning(warning);
            }
            catch (Exception error)
            {
                Debug.LogWarning(path + ": " + error.Message);
            }

            return model;
        }
    }
}
