using Swg.Client.Core.Assets.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Swg.Client.Runtime.Assets.Shaders;
using UnityEngine;
using UnityEngine.Rendering;

namespace Swg.Client.Runtime.World
{
    /// <summary>Owns shared GPU geometry and materials for the currently loaded world.</summary>
    public sealed class WorldMeshLibrary : IDisposable
    {
        sealed class ModelResources
        {
            public Mesh[] Meshes;
            public Material[] Materials;
        }

        readonly SwgMaterialFactory materials;
        readonly Shader worldShader;
        readonly Shader characterShader;
        readonly Dictionary<string, ModelResources> models = new Dictionary<string, ModelResources>(StringComparer.Ordinal);
        readonly List<UnityEngine.Object> owned = new List<UnityEngine.Object>();

        public WorldMeshLibrary(SwgMaterialFactory materials, WorldRenderingSettings settings)
        {
            this.materials = materials;
            worldShader = settings.WorldShader ?? throw new ArgumentNullException(nameof(settings.WorldShader));
            characterShader = settings.CharacterShader ?? throw new ArgumentNullException(nameof(settings.CharacterShader));
        }

        public GameObject Create(ModelData model, Transform parent)
        {
            ModelResources resources;

            if (!models.TryGetValue(model.Key, out resources))
            {
                resources = Upload(model);
                models.Add(model.Key, resources);
            }

            var instance = new GameObject(model.Key);
            instance.transform.SetParent(parent, false);

            for (int i = 0; i < resources.Meshes.Length; i++)
            {
                var part = new GameObject("Mesh " + i);
                part.transform.SetParent(instance.transform, false);
                part.AddComponent<MeshFilter>().sharedMesh = resources.Meshes[i];
                part.AddComponent<MeshRenderer>().sharedMaterial = resources.Materials[i];
            }

            return instance;
        }

        ModelResources Upload(ModelData model)
        {
            var result = new ModelResources
            {
                Meshes = new Mesh[model.Parts.Count],
                Materials = new Material[model.Parts.Count]
            };
            for (int i = 0; i < model.Parts.Count; i++)
            {
                var source = model.Parts[i];
                var mesh = Own(new Mesh { name = model.Key + " part " + i, indexFormat = IndexFormat.UInt32 });
                mesh.vertices = source.Vertices;
                if (source.Normals != null && source.Normals.Length == source.Vertices.Length)
                    mesh.normals = source.Normals;
                if (source.Uvs != null && source.Uvs.Length == source.Vertices.Length)
                    mesh.uv = source.Uvs.Select(value => new Vector2(value.x, 1 - value.y)).ToArray();
                bool hasColors = model.VertexColors != null && i < model.VertexColors.Length && model.VertexColors[i] != null && model.VertexColors[i].Length == source.Vertices.Length * 4;
                if (hasColors)
                    mesh.colors32 = Colors(model.VertexColors[i]);
                mesh.triangles = source.Triangles;
                if (source.Normals == null || source.Normals.Length != source.Vertices.Length)
                    mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                bool cutout = model.AlphaCutout != null && i < model.AlphaCutout.Length && model.AlphaCutout[i];
                var material = Own(materials.Create(model.Character ? characterShader : worldShader, source.Diffuse, source.Tint, cutout ? 0.35f : 0, hasColors, TextureWrapMode.Repeat));
                material.name = model.Key + " material " + i;
                result.Meshes[i] = mesh;
                result.Materials[i] = material;
            }

            return result;
        }

        public void PruneModels(IEnumerable<string> residentKeys)
        {
            var keep = new HashSet<string>(residentKeys, StringComparer.Ordinal);
            foreach (string key in models.Keys.Where(key => !keep.Contains(key)).ToArray())
            {
                foreach (var mesh in models[key].Meshes)
                    Release(mesh);
                foreach (var material in models[key].Materials)
                    Release(material);
                models.Remove(key);
            }
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

        T Own<T>(T resource)
            where T : UnityEngine.Object
        {
            owned.Add(resource);
            return resource;
        }

        public void Dispose()
        {
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
            models.Clear();
        }
    }
}
