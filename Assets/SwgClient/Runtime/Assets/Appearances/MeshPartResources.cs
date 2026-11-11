using System;
using System.Linq;
using Swg.Client.Core.Assets.Appearances;
using Swg.Client.Core.Geometry;
using Swg.Client.Runtime.Assets.Shaders;
using UnityEngine;
using UnityEngine.Rendering;

namespace Swg.Client.Runtime.Assets.Appearances
{
    /// <summary>Owns one uploaded mesh and material. Billboard particles only need a material.</summary>
    public sealed class MeshPartResources : IDisposable
    {
        public Mesh Mesh { get; }
        public Material Material { get; }

        public MeshPartResources(string name, MeshPart source, Material material, byte[] colors = null)
        {
            Material = material;
            Material.name = name + " material";
            if (source == null)
                return;
            Mesh = new Mesh
            {
                name = name,
                indexFormat = IndexFormat.UInt32
            };
            Mesh.vertices = source.Vertices;
            bool hasNormals = source.Normals != null && source.Normals.Length == source.Vertices.Length;
            if (hasNormals)
                Mesh.normals = source.Normals;
            if (source.Uvs != null && source.Uvs.Length == source.Vertices.Length)
                Mesh.uv = source.Uvs.Select(uv => new Vector2(uv.x, 1 - uv.y)).ToArray();
            if (colors != null && colors.Length == source.Vertices.Length * 4)
            {
                var converted = new Color32[colors.Length / 4];
                for (int i = 0; i < converted.Length; i++)
                    converted[i] = new Color32(colors[i * 4], colors[i * 4 + 1], colors[i * 4 + 2], colors[i * 4 + 3]);
                Mesh.colors32 = converted;
            }

            Mesh.triangles = source.Triangles;
            if (!hasNormals)
                Mesh.RecalculateNormals();
            Mesh.RecalculateBounds();
        }

        public static MeshPartResources FromAppearance(AppearanceData appearance, int index, SwgMaterialFactory factory, Shader shader)
        {
            var source = appearance.Parts[index];
            var colors = source.VertexColors;
            bool hasColors = colors != null && colors.Length == source.Vertices.Length * 4;
            bool cutout = source.AlphaCutout;
            var material = factory.Create(shader, source.Diffuse, source.Tint, cutout ? .35f : 0, hasColors, TextureWrapMode.Repeat);
            return new MeshPartResources(appearance.Key + " part " + index, source, material, colors);
        }

        public void Dispose()
        {
            Release(Mesh);
            Release(Material);
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
    }
}
