using System;
using System.Collections.Generic;
using System.Linq;
using Swg.Client.Core.World;
using Swg.Client.Core.World.Terrain;
using Swg.Client.Runtime.Assets.Shaders;
using UnityEngine;
using UnityEngine.Rendering;

namespace Swg.Client.Runtime.World
{
    /// <summary>Builds terrain visuals and owns their meshes and materials.</summary>
    public sealed class WorldTerrainRenderer : IDisposable
    {
        readonly SwgMaterialFactory materialsFactory;
        readonly Shader worldShader;
        readonly Vector3 renderOrigin;
        readonly List<UnityEngine.Object> owned = new List<UnityEngine.Object>();
        bool disposed;

        public WorldTerrainRenderer(SwgMaterialFactory materialsFactory, WorldState world, WorldRenderingSettings settings)
        {
            this.materialsFactory = materialsFactory;
            worldShader = settings.WorldShader;
            renderOrigin = world.RenderOrigin;
        }

        public GameObject Create(WorldTerrainPatch source, Transform parent)
        {
            ThrowIfDisposed();
            var mesh = Own(new Mesh { name = "Native terrain", indexFormat = IndexFormat.UInt32 });
            mesh.vertices = source.Vertices.Select(ToRenderPosition).ToArray();
            mesh.normals = source.Normals;
            // Keep UVs in global SWG meters so adjacent streamed patches have matching texture phase.
            mesh.uv = source.Vertices.Select(value => new Vector2(value.x, value.z)).ToArray();
            mesh.colors32 = Colors(source.Colors);
            var grouped = new Dictionary<int, List<int>>();
            for (int i = 0; i < source.Triangles.Length; i += 3)
            {
                int a = source.Triangles[i];
                int b = source.Triangles[i + 1];
                int c = source.Triangles[i + 2];
                int first = source.ShaderFamilies[a];
                int second = source.ShaderFamilies[b];
                int third = source.ShaderFamilies[c];
                int family = second == third ? second : first;
                List<int> indices;
                if (!grouped.TryGetValue(family, out indices))
                {
                    indices = new List<int>();
                    grouped.Add(family, indices);
                }

                indices.Add(a);
                indices.Add(b);
                indices.Add(c);
            }

            mesh.subMeshCount = grouped.Count;
            var materials = new Material[grouped.Count];
            int submesh = 0;
            foreach (var group in grouped)
            {
                mesh.SetTriangles(group.Value, submesh);
                var surface = source.Surfaces.FirstOrDefault(value => value.FamilyId == group.Key);
                var material = Own(materialsFactory.Create(worldShader, surface?.Diffuse, null, 0, surface?.Diffuse == null, TextureWrapMode.Repeat));
                material.name = surface?.Name ?? "Terrain family " + group.Key;
                float tileSize = surface == null || surface.TileSize <= 0 ? 8 : surface.TileSize;
                material.SetTextureScale("_BaseMap", Vector2.one / tileSize);
                materials[submesh++] = material;
            }

            mesh.RecalculateBounds();
            var terrain = new GameObject("Native terrain");
            terrain.transform.SetParent(parent, false);
            terrain.AddComponent<MeshFilter>().sharedMesh = mesh;
            terrain.AddComponent<MeshRenderer>().sharedMaterials = materials;
            return terrain;
        }

        public void Release(GameObject terrain)
        {
            if (terrain == null)
                return;
            terrain.SetActive(false);
            Release(terrain.GetComponent<MeshFilter>().sharedMesh);
            foreach (var material in terrain.GetComponent<MeshRenderer>().sharedMaterials)
                Release(material);
            UnityEngine.Object.Destroy(terrain);
        }

        void Release(UnityEngine.Object resource)
        {
            if (resource == null || !owned.Remove(resource))
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
        T Own<T>(T resource)
            where T : UnityEngine.Object
        {
            owned.Add(resource);
            return resource;
        }

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
            foreach (var resource in owned)
            {
                if (resource == null)
                    continue;
                if (Application.isPlaying)
                    UnityEngine.Object.Destroy(resource);
                else
                    UnityEngine.Object.DestroyImmediate(resource);
            }

            owned.Clear();
        }
    }
}
