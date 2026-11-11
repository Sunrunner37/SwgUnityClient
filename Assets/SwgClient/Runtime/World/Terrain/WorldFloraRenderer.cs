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
    public sealed class WorldFloraRenderer : MonoBehaviour
    {
        sealed class Resources
        {
            public MeshPartResources[] Parts;
            public (float Minimum, float Maximum) ParticleRange;
            public float Radius;
            public TerrainFloraKind Kind;
            public ParticleEffectResources[] Particles;
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
        sealed class Batch
        {
            public string Key;
            public Matrix4x4[] Matrices;
            public Matrix4x4[] Visible;
            public Vector4[] VisibleColors;
            public int VisibleCount;
            public Vector4[] Colors;
            public MaterialPropertyBlock Properties = new MaterialPropertyBlock();
            public Bounds Bounds;
        }

        readonly Dictionary<Vector2Int, List<Batch>> tiles = new Dictionary<Vector2Int, List<Batch>>();
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
        WorldStreamingSettings settings;
        public IEnumerable<Vector2Int> Keys => tiles.Keys;
        public int InstanceCount => tiles.Values.Sum(batches => batches.Sum(batch => batch.VisibleCount));
        public int TileCount => tiles.Count;

        public void Initialize(WorldState state, Shader shader, Shader particleShader, WorldStreamingSettings settings)
        {
            this.state = state;
            this.shader = shader;
            this.particleShader = particleShader;
            this.settings = settings;
            materials = GameServices.Resolve<SwgMaterialFactory>();
        }

        public void Add(Vector2Int key, TerrainFloraTile tile)
        {
            var groups = new Dictionary<string, List<Matrix4x4>>();
            var colors = new Dictionary<string, List<Vector4>>();
            var effectInstances = new List<EffectInstance>();
            foreach (var placement in tile.Placements)
            {
                var item = placement.Item;
                string resourceKey = item.Path + ":" + placement.Kind + ":" + item.Crossed + ":" + item.SwayDisplacement + ":" + item.SwayPeriod;
                if (!resources.ContainsKey(resourceKey))
                    resources.Add(resourceKey, Upload(tile.Appearances[item.Path], item, placement.Kind));
                if (!groups.TryGetValue(resourceKey, out var instances))
                {
                    groups.Add(resourceKey, instances = new List<Matrix4x4>());
                    colors.Add(resourceKey, new List<Vector4>());
                }

                var position = placement.ServerPosition - this.state.RenderOrigin;
                var scale = placement.Size * placement.Scale;
                var rotation = Quaternion.FromToRotation(Vector3.up, placement.Normal) * Quaternion.Euler(0, placement.Yaw, 0);
                if (resources[resourceKey].Particles.Length > 0)
                    effectInstances.Add(new EffectInstance { Key = resourceKey, Position = position, Rotation = rotation, Scale = scale });
                instances.Add(Matrix4x4.TRS(position, rotation, scale));
                colors[resourceKey].Add(placement.Color);
                if (item.Radial && item.Crossed)
                {
                    instances.Add(Matrix4x4.TRS(position, rotation * Quaternion.Euler(0, 90, 0), scale));
                    colors[resourceKey].Add(placement.Color);
                }
            }

            var batches = new List<Batch>();
            foreach (var group in groups)
                for (int start = 0; start < group.Value.Count; start += 1023)
                {
                    var matrices = group.Value.Skip(start).Take(1023).ToArray();
                    var bounds = new Bounds(matrices[0].GetColumn(3), Vector3.zero);
                    foreach (var matrix in matrices)
                    {
                        float scale = Math.Max(matrix.GetColumn(0).magnitude, Math.Max(matrix.GetColumn(1).magnitude, matrix.GetColumn(2).magnitude));
                        var extent = Vector3.one * resources[group.Key].Radius * scale;
                        Vector3 position = matrix.GetColumn(3);
                        bounds.Encapsulate(position - extent);
                        bounds.Encapsulate(position + extent);
                    }

                    var batch = new Batch
                    {
                        Key = group.Key,
                        Matrices = matrices,
                        Visible = new Matrix4x4[matrices.Length],
                        VisibleColors = new Vector4[matrices.Length],
                        Colors = colors[group.Key].Skip(start).Take(1023).ToArray(),
                        Bounds = bounds
                    };
                    // Allocate the full property array before clearances compact visible instances.
                    batch.Properties.SetVectorArray("_FloraTint", batch.Colors);
                    batches.Add(batch);
                }

            tiles.Add(key, batches);
            effects.Add(key, effectInstances);
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
                foreach (var batches in tiles.Values)
                    foreach (var batch in batches)
                        Filter(batch);
                exclusionsChanged = false;
            }

            if (Time.time >= nextEffectRefresh)
            {
                RefreshEffects();
                nextEffectRefresh = Time.time + .25f;
            }

            foreach (var batches in tiles.Values)
                foreach (var batch in batches)
                {
                    var instances = batch.Visible;
                    if (batch.VisibleCount == 0)
                        continue;
                    var appearance = resources[batch.Key];
                    for (int i = 0; i < appearance.Parts.Length; i++)
                    {
                        var parameters = new RenderParams(appearance.Parts[i].Material)
                        {
                            worldBounds = batch.Bounds,
                            matProps = batch.Properties,
                            // Native radial grass is drawn as foliage quads, without individual cast shadows.
                            shadowCastingMode = appearance.Kind == TerrainFloraKind.DynamicNear || appearance.Kind == TerrainFloraKind.DynamicFar ? ShadowCastingMode.Off : ShadowCastingMode.On,
                            receiveShadows = true,
                            layer = gameObject.layer
                        };
                        Graphics.RenderMeshInstanced(parameters, appearance.Parts[i].Mesh, 0, instances, batch.VisibleCount);
                    }
                }
        }

        void Filter(Batch batch)
        {
            int count = 0;
            bool collidable = resources[batch.Key].Kind == TerrainFloraKind.StaticCollidable;
            for (int i = 0; i < batch.Matrices.Length; i++)
            {
                if (clearanceIndex.Contains(batch.Matrices[i].GetColumn(3), collidable))
                    continue;
                batch.Visible[count] = batch.Matrices[i];
                batch.VisibleColors[count++] = batch.Colors[i];
            }

            batch.VisibleCount = count;
            batch.Properties.SetVectorArray("_FloraTint", batch.VisibleColors);
        }

        void RefreshEffects()
        {
            if (!ManagedSceneObject.TryGetObjectFrom(this.state.PlayerSwgId, out var player))
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

        Resources Upload(AppearanceData appearance, TerrainFloraItem item, TerrainFloraKind kind)
        {
            var distribution = this.state.Terrain.FloraDistributions[(int)kind];
            var range = settings.GetFloraRange(distribution, kind);
            var result = new Resources
            {
                Kind = kind,
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
                material.SetFloat("_FloraMaximumDistance", range.Maximum);
                material.SetFloat("_SwayHeight", Math.Max(.01f, part.Mesh.bounds.max.y));
                result.Parts[i] = part;
                result.Radius = Math.Max(result.Radius, part.Mesh.bounds.center.magnitude + part.Mesh.bounds.extents.magnitude);
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

            var used = new HashSet<string>(tiles.Values.SelectMany(batches => batches).Select(batch => batch.Key));
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
