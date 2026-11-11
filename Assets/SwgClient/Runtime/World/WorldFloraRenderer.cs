using System;
using System.Collections.Generic;
using System.Linq;
using Swg.Client.Core.Assets.Models;
using Swg.Client.Core.World;
using Swg.Client.Core.World.Terrain;
using Swg.Client.Runtime.Assets.Shaders;
using UnityEngine;
using UnityEngine.Rendering;

namespace Swg.Client.Runtime.World
{
    /// <summary>Owns streamed flora tiles and draws shared model parts in GPU-instanced batches.</summary>
    public sealed class WorldFloraRenderer : MonoBehaviour
    {
        sealed class Resources
        {
            public Mesh[] Meshes;
            public Material[] Materials;
            public float Radius;
        }
        sealed class Batch
        {
            public string Key;
            public Matrix4x4[] Matrices;
            public Bounds Bounds;
        }
        readonly Dictionary<Vector2Int, List<Batch>> tiles = new Dictionary<Vector2Int, List<Batch>>();
        readonly Dictionary<string, Resources> resources = new Dictionary<string, Resources>();
        SwgMaterialFactory materials;
        Shader shader;
        WorldState world;
        WorldStreamingSettings settings;
        public IEnumerable<Vector2Int> Keys => tiles.Keys;
        public int InstanceCount => tiles.Values.Sum(batches => batches.Sum(batch => batch.Matrices.Length));
        public int TileCount => tiles.Count;

        public void Initialize(WorldState world, Shader shader, WorldStreamingSettings settings)
        {
            this.world = world;
            this.shader = shader;
            this.settings = settings;
            materials = GameServices.Resolve<SwgMaterialFactory>();
        }
        public void Add(Vector2Int key, TerrainFloraTile tile)
        {
            var groups = new Dictionary<string, List<Matrix4x4>>();
            foreach (var placement in tile.Placements)
            {
                var item = placement.Item;
                string resourceKey = item.Path + ":" + placement.Kind + ":" + item.Crossed + ":" + item.SwayDisplacement + ":" + item.SwayPeriod;
                if (!resources.ContainsKey(resourceKey)) resources.Add(resourceKey, Upload(tile.Models[item.Path], item, placement.Kind));
                if (!groups.TryGetValue(resourceKey, out var instances)) groups.Add(resourceKey, instances = new List<Matrix4x4>());
                var position = placement.ServerPosition - world.RenderOrigin;
                var scale = (item.Radial ? new Vector3(item.Width, item.Height, item.Width) : Vector3.one) * placement.Scale;
                var rotation = Quaternion.FromToRotation(Vector3.up, placement.Normal) * Quaternion.Euler(0, placement.Yaw, 0);
                instances.Add(Matrix4x4.TRS(position, rotation, scale));
                if (item.Radial && item.Crossed) instances.Add(Matrix4x4.TRS(position, rotation * Quaternion.Euler(0, 90, 0), scale));
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
                    batches.Add(new Batch { Key = group.Key, Matrices = matrices, Bounds = bounds });
                }
            tiles.Add(key, batches);
        }

        void LateUpdate()
        {
            foreach (var batches in tiles.Values)
                foreach (var batch in batches)
                {
                    var model = resources[batch.Key];
                    for (int i = 0; i < model.Meshes.Length; i++)
                    {
                        var parameters = new RenderParams(model.Materials[i])
                        {
                            worldBounds = batch.Bounds,
                            shadowCastingMode = ShadowCastingMode.On,
                            receiveShadows = true,
                            layer = gameObject.layer
                        };
                        Graphics.RenderMeshInstanced(parameters, model.Meshes[i], 0, batch.Matrices);
                    }
                }
        }

        Resources Upload(ModelData model, TerrainFloraItem item, TerrainFloraKind kind)
        {
            var result = new Resources { Meshes = new Mesh[model.Parts.Count], Materials = new Material[model.Parts.Count] };
            for (int i = 0; i < model.Parts.Count; i++)
            {
                var source = model.Parts[i];
                var mesh = new Mesh { name = model.Key, indexFormat = IndexFormat.UInt32 };
                mesh.vertices = source.Vertices;
                mesh.normals = source.Normals;
                mesh.uv = source.Uvs.Select(uv => new Vector2(uv.x, 1 - uv.y)).ToArray();
                bool hasColors = model.VertexColors != null && model.VertexColors[i] != null;
                if (hasColors)
                {
                    var bytes = model.VertexColors[i];
                    var colors = new Color32[bytes.Length / 4];
                    for (int color = 0; color < colors.Length; color++)
                        colors[color] = new Color32(bytes[color * 4], bytes[color * 4 + 1], bytes[color * 4 + 2], bytes[color * 4 + 3]);
                    mesh.colors32 = colors;
                }
                mesh.triangles = source.Triangles;
                mesh.RecalculateBounds();
                bool cutout = model.AlphaCutout != null && model.AlphaCutout[i];
                var material = materials.Create(shader, source.Diffuse, source.Tint, cutout ? .35f : 0, hasColors, TextureWrapMode.Repeat);
                material.enableInstancing = true;
                material.SetFloat("_Billboard", item.Radial && !item.Crossed ? 1 : 0);
                material.SetFloat("_SwayDisplacement", item.SwayDisplacement);
                material.SetFloat("_SwayPeriod", item.SwayPeriod);
                material.SetFloat("_FloraMinimumDistance", kind == TerrainFloraKind.DynamicFar ? settings.nearFloraDistance : 0);
                material.SetFloat("_FloraMaximumDistance", kind == TerrainFloraKind.DynamicNear ? settings.nearFloraDistance : settings.floraRadius);
                material.SetFloat("_SwayHeight", Math.Max(.01f, mesh.bounds.max.y));
                result.Meshes[i] = mesh;
                result.Materials[i] = material;
                result.Radius = Math.Max(result.Radius, mesh.bounds.center.magnitude + mesh.bounds.extents.magnitude);
            }
            return result;
        }
        public void Retain(IEnumerable<Vector2Int> keys)
        {
            var keep = new HashSet<Vector2Int>(keys);
            foreach (var key in tiles.Keys.Where(key => !keep.Contains(key)).ToArray()) tiles.Remove(key);
            var used = new HashSet<string>(tiles.Values.SelectMany(batches => batches).Select(batch => batch.Key));
            foreach (var key in resources.Keys.Where(key => !used.Contains(key)).ToArray())
            {
                Release(resources[key]);
                resources.Remove(key);
            }
        }
        static void Release(Resources model)
        {
            foreach (var mesh in model.Meshes)
                if (Application.isPlaying) Destroy(mesh);
                else DestroyImmediate(mesh);
            foreach (var material in model.Materials)
                if (Application.isPlaying) Destroy(material);
                else DestroyImmediate(material);
        }
        void OnDestroy()
        {
            foreach (var model in resources.Values) Release(model);
            resources.Clear();
            tiles.Clear();
        }
    }
}
