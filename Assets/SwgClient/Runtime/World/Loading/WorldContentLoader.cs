using Swg.Client.Core.World.SceneObjects;
using Swg.Client.Runtime.World.Streaming;
using Swg.Client.Runtime.World.Terrain;
using Swg.Client.Core.Configuration;
using Swg.Client.Core.Networking.Game;
using Swg.Client.Core.Networking.Login;
using Swg.Client.Core;
using Swg.Client.Runtime.Assets.Tre;
using Swg.Client.Core.Assets.Appearances;
using Swg.Client.Core.Assets.Formats.Customization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Swg.Client.Core.Assets.Tre;
using Swg.Client.Core.Characters.Appearance;
using Swg.Client.Core.World;
using Swg.Client.Core.World.Terrain;
using UnityEngine;
using Stopwatch = System.Diagnostics.Stopwatch;
using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;

namespace Swg.Client.Runtime.World.Loading
{
    /// <summary>Bounded terrain preparation alongside serialized native appearance decoding, shared by entry and streaming.</summary>
    public sealed class WorldContentLoader
    {
        readonly TreRuntimeAssets assets;
        readonly WorldState state;
        readonly AppearanceCache cache;
        readonly string cacheDirectory;
        readonly string cacheServer;
        readonly string cacheUsername;
        readonly string cacheGalaxy;
        readonly FloraClearanceLoader clearance = new FloraClearanceLoader();
        byte[] lastCachedAppearance;
        readonly SemaphoreSlim reads = new SemaphoreSlim(1, 1);
        readonly SemaphoreSlim regions = new SemaphoreSlim(1, 1);
        public WorldContentLoader(TreRuntimeAssets assets, WorldState state, AppearanceCache cache, ClientSettings settings, Admission admission)
        {
            this.assets = assets;
            this.cache = cache;
            this.state = state;
            cacheDirectory = settings.AppearanceCacheDirectory;
            cacheServer = settings.LoginEndpoint;
            cacheUsername = admission.Username;
            cacheGalaxy = admission.Galaxy.Name;
        }

        public bool HasWorldMetadata => state.Templates != null;

        public async Task OpenAsync(GameWorldSnapshot initial, CancellationToken token)
        {
            var timer = Stopwatch.StartNew();
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
            this.state.Templates = loaded.templates;
            this.state.Terrain = loaded.terrain;
            this.state.Snapshot = loaded.snapshot;
            PopulateSnapshot(this.state);
            Debug.Log($"World metadata and snapshot: {timer.Elapsed.TotalSeconds:F2}s");
        }

        internal static void PopulateSnapshot(WorldState state)
        {
            foreach (var entry in state.Snapshot.Entries)
            {
                if (!state.SceneObjects.TryGet(entry.SwgId, out var existing))
                {
                    state.SceneObjects.Add(SceneObjectFactory.Create(entry));
                    continue;
                }
                // Permission packets can introduce cells before snapshot metadata is available.
                // Fill their native identity and building transform without losing live permissions.
                if (existing is not CellSceneObject cell || !AppearanceRequest.IsCell(entry.TemplatePath)) continue;
                cell.TemplatePath ??= entry.TemplatePath;
                if (cell.Number == 0) cell.Number = (int)entry.CellSwgId;
                if (cell.ParentSwgId.IsNone && !entry.ParentSwgId.IsNone)
                {
                    cell.Position = entry.Position;
                    cell.Rotation = entry.Rotation;
                    state.SceneObjects.Reparent(cell.SwgId, entry.ParentSwgId);
                }
            }
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

        public async Task<WorldRegionData> LoadRegionAsync(Vector3 serverCenter, WorldStreamingSettings settings, HashSet<Vector2Int> loadedTerrain, HashSet<SwgId> loadedScenery, IProgress<WorldPreparationProgress> progress, CancellationToken token, IReadOnlyDictionary<Vector2Int, float> terrainResolutions = null, IReadOnlyDictionary<Vector2Int, int> floraDetails = null)
        {
            // Copy settings and caller-owned sets before entering the worker.
            float tileSize = settings.terrainTileSize;
            float spacing = settings.terrainSpacing;
            float nativeSpacing = Math.Min(settings.terrainSpacing, this.state.Terrain.NativeSpacing);
            float detailRadius = settings.terrainDetailRadius;
            float midRadius = settings.terrainMidRadius;
            float farSpacing = settings.terrainFarSpacing;
            float nearFloraDistance = settings.nearFloraDistance;
            float middleFloraDistance = settings.middleFloraDistance;
            float margin = settings.refreshDistance + 64;
            var previousDetails = floraDetails == null ? new Dictionary<Vector2Int, int>() : new Dictionary<Vector2Int, int>(floraDetails);
            var resolutions = terrainResolutions == null ? null : new Dictionary<Vector2Int, float>(terrainResolutions);
            float radius = settings.terrainRadius;
            float viewRadius = settings.viewRadius;
            float floraRadius = settings.floraRadius;
            float floraTileSize = settings.floraTileSize;
            var floraRanges = this.state.Terrain.FloraDistributions.Select((distribution, kind) => settings.GetFloraRange(distribution, (TerrainFloraKind)kind)).ToArray();
            var terrain = new HashSet<Vector2Int>(loadedTerrain);
            var scenery = new HashSet<SwgId>(loadedScenery);
            await regions.WaitAsync(token);
            try
            {
                using var samples = state.Terrain.CacheRegionSamples();
                var region = new WorldRegionData
                {
                    ServerCenter = serverCenter
                };
                var total = Stopwatch.StartNew();
                int completed = 0;
                int workItems = WorldRegionPlanner.TerrainKeys(serverCenter, radius, tileSize).Count() + WorldRegionPlanner.TerrainKeys(serverCenter, floraRadius, floraTileSize).Count();
                double terrainSeconds = 0;
                double floraSeconds = 0;
                double scenerySeconds = 0;
                // Two workers at most: terrain owns surface decoding; the asset lane owns all appearance caches.
                var terrainTask = assets.ReadAsync(loader =>
                {
                    var timer = Stopwatch.StartNew();
                    region.TerrainKeys.UnionWith(WorldRegionPlanner.TerrainKeys(serverCenter, radius, tileSize));
                    foreach (var key in region.TerrainKeys)
                    {
                        token.ThrowIfCancellationRequested();
                        float Resolution(Vector2Int tile)
                        {
                            float dx = Math.Max(tile.x * tileSize - serverCenter.x, Math.Max(0, serverCenter.x - (tile.x + 1) * tileSize));
                            float dz = Math.Max(tile.y * tileSize - serverCenter.z, Math.Max(0, serverCenter.z - (tile.y + 1) * tileSize));
                            float squared = dx * dx + dz * dz;
                            return squared <= detailRadius * detailRadius ? nativeSpacing : squared <= midRadius * midRadius ? spacing : farSpacing;
                        }

                        float previous = resolutions != null && resolutions.TryGetValue(key, out var old) ? old : spacing;
                        float OldNeighbor(Vector2Int tile) => resolutions != null && resolutions.TryGetValue(tile, out var oldSpacing) ? oldSpacing : spacing;
                        float desired = Resolution(key);
                        var neighbors = new[]
                        {
                            key + Vector2Int.left,
                            key + Vector2Int.right,
                            key + Vector2Int.down,
                            key + Vector2Int.up
                        };
                        var neighborSpacings = new Vector4(Resolution(neighbors[0]), Resolution(neighbors[1]), Resolution(neighbors[2]), Resolution(neighbors[3]));
                        bool edgesChanged = false;
                        for (int i = 0; i < 4; i++)
                            edgesChanged |= Math.Max(previous, OldNeighbor(neighbors[i])) != Math.Max(desired, neighborSpacings[i]);
                        if (!terrain.Contains(key) || (resolutions != null && (previous != desired || edgesChanged)))
                            region.Terrain.Add(key, WorldTerrainPatch.Build(this.state.Terrain, new Vector3((key.x + .5f) * tileSize, 0, (key.y + .5f) * tileSize), tileSize * .5f, desired, neighborSpacings));
                        progress?.Report(new WorldPreparationProgress("Reading original terrain...", .9f * Interlocked.Increment(ref completed) / workItems));
                    }

                    terrainSeconds = timer.Elapsed.TotalSeconds;
                    return true;
                });
                var assetTask = ReadAsync(loader =>
                {
                    var timer = Stopwatch.StartNew();
                    region.FloraKeys.UnionWith(WorldRegionPlanner.TerrainKeys(serverCenter, floraRadius, floraTileSize));
                    var floraLoader = new TerrainFloraLoader(loader, this.state.Terrain, cache, path => LoadAppearance(loader, new AppearanceRequest(path)));
                    foreach (var key in region.FloraKeys)
                    {
                        token.ThrowIfCancellationRequested();
                        float dx = Math.Max(key.x * floraTileSize - serverCenter.x, Math.Max(0, serverCenter.x - (key.x + 1) * floraTileSize));
                        float dz = Math.Max(key.y * floraTileSize - serverCenter.z, Math.Max(0, serverCenter.z - (key.y + 1) * floraTileSize));
                        float distance = (float)Math.Sqrt(dx * dx + dz * dz);
                        int detail = distance <= nearFloraDistance + margin ? 0 : distance <= middleFloraDistance + margin ? 1 : 2;
                        if (!previousDetails.TryGetValue(key, out int previousDetail) || previousDetail != detail)
                            region.Flora.Add(key, floraLoader.LoadTile(key, floraTileSize, floraRanges, token, detail));
                        progress?.Report(new WorldPreparationProgress("Preparing original flora...", .9f * Interlocked.Increment(ref completed) / workItems));
                    }

                    var entries = this.state.Snapshot.Query(serverCenter, viewRadius, 256).ToArray();
                    floraSeconds = timer.Elapsed.TotalSeconds;
                    timer.Restart();
                    int count = 0;
                    foreach (var entry in entries)
                    {
                        token.ThrowIfCancellationRequested();
                        if (++count % 100 == 0)
                            progress?.Report(new WorldPreparationProgress("Loading scenery " + count + "/" + entries.Length, .9f + .1f * count / entries.Length));
                        if (AppearanceRequest.IsCell(entry.TemplatePath))
                            continue;
                        if (scenery.Contains(entry.SwgId))
                        {
                            region.ScenerySwgIds.Add(entry.SwgId);
                            continue;
                        }

                        var appearance = LoadAppearance(loader, new AppearanceRequest(entry.TemplatePath));
                        if (appearance.Parts.Count == 0 && appearance.Particles.Count == 0)
                            continue;
                        region.ScenerySwgIds.Add(entry.SwgId);
                        region.Scenery.Add(entry.SwgId, appearance);
                    }

                    scenerySeconds = timer.Elapsed.TotalSeconds;
                    return true;
                }, token);
                await Task.WhenAll(terrainTask, assetTask);
                Debug.Log($"World region preparation: {total.Elapsed.TotalSeconds:F2}s wall; terrain {terrainSeconds:F2}s, flora {floraSeconds:F2}s, scenery {scenerySeconds:F2}s; {region.Terrain.Count} terrain / {region.Flora.Count} flora tiles");
                progress?.Report(new WorldPreparationProgress("Region assets prepared", 1));
                return region;
            }
            finally
            {
                regions.Release();
            }
        }

        public Task<AppearanceData> LoadAppearanceAsync(SceneObject sceneObject) => LoadAppearanceAsync(DescribeAppearance(sceneObject));

        public AppearanceRequest DescribeAppearance(SceneObject sceneObject)
        {
            // Capture live object inputs on the main thread before decoding assets on a worker.
            var request = AppearanceRequest.Describe(state, sceneObject);
            if (request == null)
            {
                CharacterAppearance character = null;
                if (sceneObject is TangibleSceneObject tangible && sceneObject.TemplatePath.Contains("/wearables/"))
                    character = new CharacterAppearance { TemplatePath = sceneObject.TemplatePath, Customization = PackedCustomization.Decode(tangible.Customization), Wearables = Array.Empty<WearableAppearance>() };
                request = new AppearanceRequest(sceneObject.TemplatePath, character);
            }
            return request;
        }

        public Task<AppearanceData> LoadAppearanceAsync(AppearanceRequest request) => ReadAsync(loader => LoadAppearance(loader, request), CancellationToken.None);

        public Task<Dictionary<SwgId, AppearanceData>> LoadAppearancesAsync(GameWorldSnapshot snapshot, IReadOnlyDictionary<SwgId, AppearanceRequest> requests, CancellationToken token)
        {
            return ReadAsync(loader =>
            {
                var timer = Stopwatch.StartNew();
                var appearances = new Dictionary<SwgId, AppearanceData>();
                foreach (var pair in requests)
                {
                    token.ThrowIfCancellationRequested();
                    try
                    {
                        var appearance = LoadAppearance(loader, pair.Value);
                        if (appearance.Parts.Count != 0 || appearance.Particles.Count != 0)
                            appearances.Add(pair.Key, appearance);
                    }
                    catch (Exception error)
                    {
                        Debug.LogWarning("SceneObject " + pair.Key + ": " + error.Message);
                    }
                }

                CachePlayerAppearance(snapshot);
                Debug.Log($"World object appearances: {timer.Elapsed.TotalSeconds:F2}s; {appearances.Count} objects");
                return appearances;
            }, token);
        }

        void CachePlayerAppearance(GameWorldSnapshot snapshot)
        {
            var player = snapshot.Objects.FirstOrDefault(item => item.SwgId == snapshot.PlayerSwgId);
            if (player == null || player is not CreatureSceneObject || !snapshot.PlayerBaselinesComplete)
                return;
            string path = this.state.Templates.Resolve(player.TemplateCrc) ?? player.TemplatePath;
            if (string.IsNullOrEmpty(path))
                return;
            try
            {
                var appearance = AppearanceRequest.Describe(this.state, snapshot, player)?.Character;
                if (appearance == null)
                    return;
                string planet = Path.GetFileNameWithoutExtension(this.state.TerrainPath ?? "");
                var bytes = CharacterAppearanceCache.Serialize(appearance, planet);
                if (lastCachedAppearance != null && bytes.SequenceEqual(lastCachedAppearance))
                    return;
                CharacterAppearanceCache.SaveProfile(cacheDirectory, cacheServer, cacheUsername, cacheGalaxy, player.SwgId, appearance, planet);
                lastCachedAppearance = bytes;
            }
            catch (Exception error)when (error is IOException || error is UnauthorizedAccessException || error is InvalidDataException || error is ArgumentException)
            {
                Debug.LogWarning("Character appearance cache could not be saved: " + error.Message);
            }
        }

        public FloraClearance[] GetFloraClearance(string templatePath) => clearance.GetCached(templatePath);
        AppearanceData LoadAppearance(TreAssetLoader loader, AppearanceRequest request)
        {
            string path = request.TemplatePath;
            clearance.Load(loader, path);
            if (cache.TryGet(request.Key, out var cached))
                return cached;
            var result = new AppearanceData
            {
                Key = request.Key
            };
            try
            {
                var characterAppearance = request.Character;
                if (characterAppearance == null)
                {
                    result = AppearanceLoader.LoadDetailed(loader, path);
                    result.Key = request.Key;
                    foreach (var warning in result.Warnings)
                        Debug.LogWarning(warning);
                    if (result.Parts.Count != 0 || result.Particles.Count != 0 || !(path.Contains("/creature/") || path.Contains("/wearables/")))
                    {
                        cache.Add(request.Key, result);
                        return result;
                    }

                    characterAppearance = new CharacterAppearance
                    {
                        TemplatePath = path,
                        Customization = PackedCustomization.Decode(Array.Empty<byte>()),
                        Wearables = Array.Empty<WearableAppearance>(),
                    };
                }

                var character = SkeletalAppearanceLoader.LoadDetailed(loader, characterAppearance);
                result.Parts = character.Parts;
                result.Particles = character.Particles;
                result.RiderSeat = character.RiderSeat;
                result.RecalculateBounds();
            }
            catch (Exception error)
            {
                Debug.LogWarning(path + ": " + error.Message);
            }

            cache.Add(request.Key, result);
            return result;
        }
    }
}
