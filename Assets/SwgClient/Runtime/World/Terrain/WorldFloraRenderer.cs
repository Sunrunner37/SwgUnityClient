using Swg.Client.Core.Characters;
using Swg.Client.Runtime.Assets.Appearances;
using Swg.Client.Runtime.World.SceneObjects;
using Swg.Client.Runtime.World.Streaming;
using System;
using Swg.Client.Runtime.Assets.Particles;
using Swg.Client.Core;
using System.Collections.Generic;
using System.Linq;
using Swg.Client.Core.Assets.Appearances;
using Swg.Client.Core.World;
using Swg.Client.Core.World.Terrain;
using Swg.Client.Runtime.Assets.Shaders;
using UnityEngine;
using UnityEngine.Rendering;

namespace Swg.Client.Runtime.World.Terrain
{
    /// <summary>Owns streamed flora tiles and draws shared appearance parts in GPU-instanced batches.</summary>
    [DefaultExecutionOrder(100)]
    public sealed class WorldFloraRenderer : MonoBehaviour
    {
        // Non-uniform native grass scaling requires both object and inverse transforms.
        const int InstanceBatchSize = 511;
        sealed class Resources
        {
            public MeshPartResources[] Parts;
            public float MaximumDistance;
            public int Detail;
            public Submission Visible;
            public Submission Shadows;
            public (float Minimum, float Maximum) ParticleRange;
            public float Radius;
            public TerrainFloraKind Kind;
            public ParticleEffectResources[] Particles;
        }

        sealed class Submission
        {
            public readonly Matrix4x4[] Matrices = new Matrix4x4[InstanceBatchSize];
            public readonly Vector4[] Colors = new Vector4[InstanceBatchSize];
            public readonly MaterialPropertyBlock Properties = new MaterialPropertyBlock();
            public Bounds Bounds;
            public int Count;
        }

        sealed class EffectInstance
        {
            public string Key;
            public Vector3 Position;
            public Quaternion Rotation;
            public Vector3 Scale;
            public GameObject Object;
        }

        readonly Dictionary<Vector2Int, List<EffectInstance>> effects = new Dictionary<Vector2Int, List<EffectInstance>>();
        FloraClearanceIndex clearanceIndex = new FloraClearanceIndex(Array.Empty<FloraClearance>());
        float nextEffectRefresh;
        Shader particleShader;
        Camera worldCamera;
        readonly Plane[] frustum = new Plane[6];
        public int SubmittedBatches { get; private set; }
        public int SubmittedInstances { get; private set; }

        sealed class Tile
        {
            public int Detail;
            public Bounds Bounds;
            public List<Batch> Batches;
        }

        sealed class Batch
        {
            public string Key;
            public Resources Appearance;
            public Matrix4x4[] Matrices;
            public Matrix4x4[] Visible;
            public Vector4[] VisibleColors;
            public int VisibleCount;
            public Vector4[] Colors;
            public Bounds Bounds;
        }

        readonly Dictionary<Vector2Int, Tile> tiles = new Dictionary<Vector2Int, Tile>();
        readonly Dictionary<string, Resources> resources = new Dictionary<string, Resources>();
        readonly Dictionary<SwgId, FloraClearance[]> exclusions = new Dictionary<SwgId, FloraClearance[]>();
        bool exclusionsChanged;
        public void SetExclusion(SwgId swgId, FloraClearance[] circles)
        {
            exclusions[swgId] = circles;
            exclusionsChanged = true;
        }

        public void RemoveExclusion(SwgId swgId)
        {
            if (exclusions.Remove(swgId))
                exclusionsChanged = true;
        }

        SwgMaterialFactory materials;
        Shader shader;
        WorldState state;
        CurrentPlayer currentPlayer;
        WorldStreamingSettings settings;
        public IEnumerable<Vector2Int> Keys => tiles.Keys;
        public int InstanceCount => tiles.Values.Sum(tile => tile.Batches.Sum(batch => batch.VisibleCount));
        public int TileCount => tiles.Count;
        public IReadOnlyDictionary<Vector2Int, int> Details => tiles.ToDictionary(pair => pair.Key, pair => pair.Value.Detail);

        public void Initialize(WorldState state, Shader shader, Shader particleShader, WorldStreamingSettings settings, Camera worldCamera = null)
        {
            currentPlayer = GameServices.Resolve<CurrentPlayer>();
            this.state = state;
            this.shader = shader;
            this.particleShader = particleShader;
            this.settings = settings;
            this.worldCamera = worldCamera;
            materials = GameServices.Resolve<SwgMaterialFactory>();
        }

        public void AddOrReplace(Vector2Int key, TerrainFloraTile tile)
        {
            var groups = new Dictionary<(string Resource, int X, int Z), List<Matrix4x4>>();
            var colors = new Dictionary<(string Resource, int X, int Z), List<Vector4>>();
            var effectInstances = new List<EffectInstance>();
            foreach (var placement in tile.Placements)
            {
                var item = placement.Item;
                string resourceKey = placement.AppearanceKey + ":" + placement.Detail + ":" + placement.Kind + ":" + item.Crossed + ":" + item.SwayDisplacement + ":" + item.SwayPeriod;
                if (!resources.ContainsKey(resourceKey))
                    resources.Add(resourceKey, Upload(tile.Appearances[placement.AppearanceKey], item, placement.Kind, placement.Detail));
                var groupKey = (resourceKey, Mathf.FloorToInt(placement.ServerPosition.x / 32), Mathf.FloorToInt(placement.ServerPosition.z / 32));
                if (!groups.TryGetValue(groupKey, out var instances))
                {
                    groups.Add(groupKey, instances = new List<Matrix4x4>());
                    colors.Add(groupKey, new List<Vector4>());
                }

                var position = placement.ServerPosition - this.state.RenderOrigin;
                var scale = placement.Size * placement.Scale;
                var rotation = Quaternion.FromToRotation(Vector3.up, placement.Normal) * Quaternion.Euler(0, placement.Yaw, 0);
                if (resources[resourceKey].Particles.Length > 0)
                    effectInstances.Add(new EffectInstance { Key = resourceKey, Position = position, Rotation = rotation, Scale = scale });
                instances.Add(Matrix4x4.TRS(position, rotation, scale));
                colors[groupKey].Add(placement.Color);
                if (item.Radial && item.Crossed)
                {
                    instances.Add(Matrix4x4.TRS(position, rotation * Quaternion.Euler(0, 90, 0), scale));
                    colors[groupKey].Add(placement.Color);
                }
            }

            var batches = new List<Batch>();
            foreach (var group in groups)
                for (int start = 0; start < group.Value.Count; start += InstanceBatchSize)
                {
                    var matrices = group.Value.Skip(start).Take(InstanceBatchSize).ToArray();
                    var bounds = new Bounds(matrices[0].GetColumn(3), Vector3.zero);
                    foreach (var matrix in matrices)
                    {
                        float scale = Math.Max(matrix.GetColumn(0).magnitude, Math.Max(matrix.GetColumn(1).magnitude, matrix.GetColumn(2).magnitude));
                        var extent = Vector3.one * resources[group.Key.Resource].Radius * scale;
                        Vector3 position = matrix.GetColumn(3);
                        bounds.Encapsulate(position - extent);
                        bounds.Encapsulate(position + extent);
                    }

                    var batch = new Batch
                    {
                        Key = group.Key.Resource,
                        Appearance = resources[group.Key.Resource],
                        Matrices = matrices,
                        Visible = new Matrix4x4[matrices.Length],
                        VisibleColors = new Vector4[matrices.Length],
                        Colors = colors[group.Key].Skip(start).Take(InstanceBatchSize).ToArray(),
                        Bounds = bounds
                    };
                    batches.Add(batch);
                }

            if (effects.TryGetValue(key, out var previousEffects))
                foreach (var effect in previousEffects)
                    if (effect.Object != null)
                        Destroy(effect.Object);
            var tileBounds = new Bounds(new Vector3((key.x + .5f) * settings.floraTileSize - state.RenderOrigin.x, 0, (key.y + .5f) * settings.floraTileSize - state.RenderOrigin.z), Vector3.zero);
            foreach (var batch in batches)
                tileBounds.Encapsulate(batch.Bounds);
            tiles[key] = new Tile
            {
                Detail = tile.Detail,
                Bounds = tileBounds,
                Batches = batches
            };
            effects[key] = effectInstances;
            if (!exclusionsChanged)
                foreach (var batch in batches)
                    Filter(batch);
            nextEffectRefresh = 0;
        }

        void LateUpdate()
        {
            if (exclusionsChanged)
            {
                clearanceIndex = new FloraClearanceIndex(exclusions.Values.SelectMany(value => value));
                nextEffectRefresh = 0;
                foreach (var tile in tiles.Values)
                    foreach (var batch in tile.Batches)
                        Filter(batch);
                exclusionsChanged = false;
            }

            if (Time.time >= nextEffectRefresh)
            {
                RefreshEffects();
                nextEffectRefresh = Time.time + .25f;
            }

            SubmittedBatches = 0;
            SubmittedInstances = 0;
            bool cull = worldCamera != null;
            Vector3 cameraPosition = cull ? worldCamera.transform.position : Vector3.zero;
            if (cull)
                GeometryUtility.CalculateFrustumPlanes(worldCamera, frustum);
            Shader.SetGlobalVector("_SwgFloraCamera", cameraPosition);
            foreach (var tile in tiles.Values)
            {
                if (cull && !GeometryUtility.TestPlanesAABB(frustum, tile.Bounds) && (cameraPosition - tile.Bounds.ClosestPoint(cameraPosition)).sqrMagnitude > settings.nearFloraDistance * settings.nearFloraDistance)
                    continue;
                foreach (var batch in tile.Batches)
                {
                    if (batch.VisibleCount == 0)
                        continue;
                    var appearance = batch.Appearance;
                    float nearest = cull ? Vector3.SqrMagnitude(cameraPosition - batch.Bounds.ClosestPoint(cameraPosition)) : 0;
                    if (nearest > appearance.MaximumDistance * appearance.MaximumDistance)
                        continue;
                    bool radial = appearance.Kind == TerrainFloraKind.DynamicNear || appearance.Kind == TerrainFloraKind.DynamicFar;
                    bool shadow = !radial && appearance.Detail == 0 && nearest <= settings.nearFloraDistance * settings.nearFloraDistance;
                    if (cull && !shadow && !GeometryUtility.TestPlanesAABB(frustum, batch.Bounds))
                        continue;
                    if (appearance.Parts.Length > 0)
                        Accumulate(appearance, shadow ? appearance.Shadows : appearance.Visible, batch, shadow);
                }
            }

            foreach (var appearance in resources.Values)
            {
                if (appearance.Visible != null)
                    Flush(appearance, appearance.Visible, false);
                if (appearance.Shadows != null)
                    Flush(appearance, appearance.Shadows, true);
            }
        }

        void Accumulate(Resources appearance, Submission submission, Batch batch, bool shadow)
        {
            int start = 0;
            while (start < batch.VisibleCount)
            {
                int count = Math.Min(InstanceBatchSize - submission.Count, batch.VisibleCount - start);
                Array.Copy(batch.Visible, start, submission.Matrices, submission.Count, count);
                Array.Copy(batch.VisibleColors, start, submission.Colors, submission.Count, count);
                if (submission.Count == 0)
                    submission.Bounds = batch.Bounds;
                else
                    submission.Bounds.Encapsulate(batch.Bounds);
                submission.Count += count;
                start += count;
                if (submission.Count == InstanceBatchSize)
                    Flush(appearance, submission, shadow);
            }
        }

        void Flush(Resources appearance, Submission submission, bool shadow)
        {
            if (submission.Count == 0)
                return;
            submission.Properties.SetVectorArray("_FloraTint", submission.Colors);
            foreach (var part in appearance.Parts)
            {
                var parameters = new RenderParams(part.Material)
                {
                    worldBounds = submission.Bounds,
                    matProps = submission.Properties,
                    shadowCastingMode = shadow ? ShadowCastingMode.On : ShadowCastingMode.Off,
                    receiveShadows = true,
                    layer = gameObject.layer
                };
                Graphics.RenderMeshInstanced(parameters, part.Mesh, 0, submission.Matrices, submission.Count);
                SubmittedBatches++;
                SubmittedInstances += submission.Count;
            }

            submission.Count = 0;
        }

        void Filter(Batch batch)
        {
            int count = 0;
            bool collidable = batch.Appearance.Kind == TerrainFloraKind.StaticCollidable;
            for (int i = 0; i < batch.Matrices.Length; i++)
            {
                if (clearanceIndex.Contains(batch.Matrices[i].GetColumn(3), collidable))
                    continue;
                batch.Visible[count] = batch.Matrices[i];
                batch.VisibleColors[count++] = batch.Colors[i];
            }

            batch.VisibleCount = count;
        }

        void RefreshEffects()
        {
            if (!ManagedSceneObject.TryGetObjectFrom(currentPlayer.SwgId, out var player))
                return;
            foreach (var effect in effects.Values.SelectMany(tile => tile))
            {
                var resource = resources[effect.Key];
                float distance = Vector3.Distance(player.transform.position, effect.Position);
                var range = resource.ParticleRange;
                bool visible = distance >= range.Minimum && distance <= range.Maximum && !clearanceIndex.Contains(effect.Position, resource.Kind == TerrainFloraKind.StaticCollidable);
                if (visible && effect.Object == null)
                {
                    effect.Object = new GameObject("Particle flora");
                    effect.Object.transform.SetParent(transform, false);
                    effect.Object.transform.localPosition = effect.Position;
                    effect.Object.transform.localRotation = effect.Rotation;
                    effect.Object.transform.localScale = effect.Scale;
                    foreach (var particle in resource.Particles)
                        particle.Create(effect.Object.transform);
                }
                else if (!visible && effect.Object != null)
                {
                    Destroy(effect.Object);
                    effect.Object = null;
                }
            }
        }

        Resources Upload(AppearanceData appearance, TerrainFloraItem item, TerrainFloraKind kind, int detail)
        {
            var distribution = this.state.Terrain.FloraDistributions[(int)kind];
            var range = settings.GetFloraRange(distribution, kind);
            if (kind == TerrainFloraKind.DynamicNear)
                range.Maximum = settings.GrassDistance(detail);
            else if (kind != TerrainFloraKind.DynamicFar)
                range.Maximum = Mathf.Min(range.Maximum, Mathf.Max(128, appearance.Radius * 96));
            var result = new Resources
            {
                Kind = kind,
                Detail = detail,
                MaximumDistance = range.Maximum,
                ParticleRange = settings.GetFloraRange(distribution, kind, particles: true),
                Particles = appearance.Particles.Select(effect => new ParticleEffectResources(effect, materials, shader, shader, particleShader)).ToArray(),
                Parts = new MeshPartResources[appearance.Parts.Count]
            };
            for (int i = 0; i < result.Parts.Length; i++)
            {
                var part = MeshPartResources.FromAppearance(appearance, i, materials, shader);
                var material = part.Material;
                material.enableInstancing = true;
                material.SetFloat("_Billboard", kind == TerrainFloraKind.DynamicFar && !item.Crossed ? 1 : 0);
                material.SetFloat("_SwayDisplacement", item.SwayDisplacement);
                material.SetFloat("_SwayPeriod", item.SwayPeriod);
                material.SetFloat("_FloraMinimumDistance", range.Minimum);
                material.SetFloat("_FloraFadeDistance", settings.floraFadeDistance);
                material.SetFloat("_FloraShadowDistance", settings.nearFloraDistance);
                material.SetFloat("_FloraWidthScale", kind == TerrainFloraKind.DynamicNear ? 1 << detail : 1);
                material.SetFloat("_FloraWidthStart", settings.nearFloraDistance * .5f);
                material.SetFloat("_FloraWidthEnd", settings.middleFloraDistance);
                material.SetFloat("_FloraMaximumDistance", range.Maximum);
                material.SetFloat("_SwayHeight", Math.Max(.01f, part.Mesh.bounds.max.y));
                result.Parts[i] = part;
                result.Radius = Math.Max(result.Radius, part.Mesh.bounds.center.magnitude + part.Mesh.bounds.extents.magnitude * (kind == TerrainFloraKind.DynamicNear ? 1 << detail : 1) + Mathf.Abs(item.SwayDisplacement));
            }

            if (result.Parts.Length > 0)
            {
                result.Visible = new Submission();
                if (detail == 0 && kind != TerrainFloraKind.DynamicNear && kind != TerrainFloraKind.DynamicFar)
                    result.Shadows = new Submission();
            }

            return result;
        }

        public void Retain(IEnumerable<Vector2Int> keys)
        {
            var keep = new HashSet<Vector2Int>(keys);
            foreach (var key in tiles.Keys.Where(key => !keep.Contains(key)).ToArray())
            {
                foreach (var effect in effects[key])
                    if (effect.Object != null)
                        Destroy(effect.Object);
                effects.Remove(key);
                tiles.Remove(key);
            }

            var used = new HashSet<string>(tiles.Values.SelectMany(tile => tile.Batches).Select(batch => batch.Key));
            foreach (var key in resources.Keys.Where(key => !used.Contains(key)).ToArray())
            {
                Release(resources[key]);
                resources.Remove(key);
            }
        }

        static void Release(Resources appearance)
        {
            foreach (var effect in appearance.Particles)
                effect.Dispose();
            foreach (var part in appearance.Parts)
                part.Dispose();
        }

        void OnDestroy()
        {
            foreach (var appearance in resources.Values)
                Release(appearance);
            resources.Clear();
            tiles.Clear();
        }
    }
}
