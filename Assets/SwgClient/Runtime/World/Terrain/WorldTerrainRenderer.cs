using System;
using System.Collections.Generic;
using System.Linq;
using Swg.Client.Core.World;
using Swg.Client.Core.World.Terrain;
using Swg.Client.Runtime.Assets.Shaders;
using UnityEngine;
using UnityEngine.Rendering;

namespace Swg.Client.Runtime.World.Terrain
{
    /// <summary>Builds terrain visuals and owns their meshes and materials.</summary>
    public sealed class WorldTerrainRenderer : IDisposable
    {
        readonly SwgMaterialFactory materialsFactory;
        readonly Shader worldShader;
        readonly Vector3 renderOrigin;
        sealed class Tile
        {
            public GameObject Object;
            public Mesh Mesh;
            public Material[] Materials;
            public float Spacing;
        }

        readonly Dictionary<Vector2Int, Tile> tiles = new Dictionary<Vector2Int, Tile>();
        public IEnumerable<Vector2Int> Keys => tiles.Keys;
        public IReadOnlyDictionary<Vector2Int, float> Resolutions => tiles.ToDictionary(pair => pair.Key, pair => pair.Value.Spacing);

        bool disposed;
        public WorldTerrainRenderer(SwgMaterialFactory materialsFactory, WorldState state, WorldRenderingContext context)
        {
            this.materialsFactory = materialsFactory;
            worldShader = context.WorldShader;
            renderOrigin = state.RenderOrigin;
        }

        public void AddOrReplace(Vector2Int key, WorldTerrainPatch source, Transform parent)
        {
            ThrowIfDisposed();
            var replacement = Create(source, parent);
            if (tiles.TryGetValue(key, out var previous))
                Release(previous);
            tiles[key] = replacement;
        }

        public void Retain(IEnumerable<Vector2Int> keys)
        {
            var keep = new HashSet<Vector2Int>(keys);
            foreach (var key in tiles.Keys.Where(key => !keep.Contains(key)).ToArray())
            {
                Release(tiles[key]);
                tiles.Remove(key);
            }
        }

        Tile Create(WorldTerrainPatch source, Transform parent)
        {
            ThrowIfDisposed();
            var mesh = new Mesh
            {
                name = "Native terrain",
                indexFormat = IndexFormat.UInt32
            };
            var vertices = new List<Vector3>(source.Vertices.Length);
            var normals = new List<Vector3>(source.Vertices.Length);
            var uvs = new List<Vector2>(source.Vertices.Length);
            var colors = new List<Color32>(source.Vertices.Length);
            var weights = new List<Vector3>(source.Vertices.Length);
            var sourceColors = Colors(source.Colors);
            var grouped = new Dictionary<(int First, int Second, int Third), (List<int> Indices, Dictionary<int, int> Vertices)>();
            for (int i = 0; i < source.Triangles.Length; i += 3)
            {
                var key = FamilyKey(source.ShaderFamilies[source.Triangles[i]], source.ShaderFamilies[source.Triangles[i + 1]], source.ShaderFamilies[source.Triangles[i + 2]]);
                if (!grouped.TryGetValue(key, out var group))
                {
                    group = (new List<int>(), new Dictionary<int, int>());
                    grouped.Add(key, group);
                }

                for (int corner = 0; corner < 3; corner++)
                {
                    int index = source.Triangles[i + corner];
                    if (!group.Vertices.TryGetValue(index, out int vertex))
                    {
                        vertex = vertices.Count;
                        group.Vertices.Add(index, vertex);
                        vertices.Add(ToRenderPosition(source.Vertices[index]));
                        normals.Add(source.Normals[index]);
                        uvs.Add(new Vector2(source.Vertices[index].x, source.Vertices[index].z));
                        colors.Add(sourceColors[index]);
                        int family = source.ShaderFamilies[index];
                        weights.Add(family == key.Item1 ? Vector3.right : family == key.Item2 ? Vector3.up : Vector3.forward);
                    }

                    group.Indices.Add(vertex);
                }
            }

            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetUVs(1, weights);
            mesh.SetColors(colors);
            mesh.subMeshCount = grouped.Count;
            var materials = new Material[grouped.Count];
            int submesh = 0;
            foreach (var group in grouped)
            {
                mesh.SetTriangles(group.Value.Indices, submesh);
                TerrainSurface Surface(int id) => source.Surfaces.FirstOrDefault(value => value.FamilyId == id);
                var first = Surface(group.Key.First);
                var material = materialsFactory.Create(worldShader, first?.Diffuse, null, 0, true, TextureWrapMode.Repeat);
                material.name = first?.Name ?? "Terrain family " + group.Key.First;
                material.SetFloat("_TerrainBlend", 1);
                material.SetTextureScale("_BaseMap", Vector2.one / (first == null || first.TileSize <= 0 ? 8 : first.TileSize));
                void Layer(string property, int family)
                {
                    var surface = Surface(family);
                    material.SetTexture(property, materialsFactory.Texture(surface?.Diffuse, TextureWrapMode.Repeat));
                    material.SetTextureScale(property, Vector2.one / (surface == null || surface.TileSize <= 0 ? 8 : surface.TileSize));
                }

                Layer("_TerrainMap1", group.Key.Second);
                Layer("_TerrainMap2", group.Key.Third);
                materials[submesh++] = material;
            }

            mesh.RecalculateBounds();
            var terrain = new GameObject("Native terrain");
            terrain.transform.SetParent(parent, false);
            terrain.AddComponent<MeshFilter>().sharedMesh = mesh;
            terrain.AddComponent<MeshRenderer>().sharedMaterials = materials;
            return new Tile
            {
                Object = terrain,
                Mesh = mesh,
                Materials = materials,
                Spacing = source.Spacing
            };
        }

        static (int First, int Second, int Third) FamilyKey(int first, int second, int third)
        {
            if (first > second)
                (first, second) = (second, first);
            if (second > third)
                (second, third) = (third, second);
            if (first > second)
                (first, second) = (second, first);
            if (first == second)
                return (first, third, first);
            return (first, second, second == third ? first : third);
        }

        static void Release(Tile tile)
        {
            if (tile.Object != null)
                tile.Object.SetActive(false);
            Release(tile.Mesh);
            foreach (var material in tile.Materials)
                Release(material);
            Release(tile.Object);
        }

        static void Release(UnityEngine.Object resource)
        {
            if (resource == null)
                return;
            if (Application.isPlaying)
                UnityEngine.Object.Destroy(resource);
            else
                UnityEngine.Object.DestroyImmediate(resource);
        }

        static Color32[] Colors(byte[] bytes)
        {
            var result = new Color32[bytes.Length / 4];
            for (int i = 0; i < result.Length; i++)
                result[i] = new Color32(bytes[i * 4], bytes[i * 4 + 1], bytes[i * 4 + 2], bytes[i * 4 + 3]);
            return result;
        }

        Vector3 ToRenderPosition(Vector3 serverPosition) => serverPosition - renderOrigin;
        void ThrowIfDisposed()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(WorldTerrainRenderer));
        }

        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            foreach (var tile in tiles.Values)
                Release(tile);
            tiles.Clear();
        }
    }
}
