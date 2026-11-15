using System;
using System.Linq;
using Swg.Client.Core.Assets.Appearances;
using Swg.Client.Core.Geometry;
using Swg.Client.Runtime.Assets.Shaders;
using UnityEngine;
using Unity.Collections;
using Swg.Client.Core.Characters.Animation;
using UnityEngine.Rendering;

namespace Swg.Client.Runtime.Assets.Appearances
{
    /// <summary>Owns one uploaded mesh and material. Billboard particles only need a material.</summary>
    public sealed class MeshPartResources : IDisposable
    {
        public Mesh Mesh { get; }
        public Material Material { get; }
        public Bounds Bounds { get; }
        public SkeletalMeshData Skinning { get; }
        public Texture[] AnimationFrames { get; private set; }
        public float MinimumFrameSeconds { get; private set; }
        public float MaximumFrameSeconds { get; private set; }

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
            Skinning = source.Skinning;
            Mesh.vertices = Skinning?.Vertices ?? source.Vertices;
            bool hasNormals = source.Normals != null && source.Normals.Length == source.Vertices.Length;
            if (hasNormals)
                Mesh.normals = Skinning?.Normals ?? source.Normals;
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
            var bounds = Mesh.bounds;
            if (Skinning != null)
            {
                Mesh.bindposes = Skinning.Skeleton.BindPoses;
                using var counts = new NativeArray<byte>(Skinning.BonesPerVertex, Allocator.Temp);
                using var weights = new NativeArray<BoneWeight1>(Skinning.Weights, Allocator.Temp);
                Mesh.SetBoneWeights(counts, weights);
                bounds = new Bounds(source.Vertices[0], Vector3.zero);
                foreach (var vertex in source.Vertices)
                    bounds.Encapsulate(vertex);
            }

            Bounds = bounds;
        }

        public static MeshPartResources FromAppearance(AppearanceData appearance, int index, SwgMaterialFactory factory, Shader shader, TextureWrapMode wrapMode = TextureWrapMode.Repeat)
        {
            var source = appearance.Parts[index];
            var colors = source.VertexColors;
            bool hasColors = colors != null && colors.Length == source.Vertices.Length * 4;
            bool cutout = source.AlphaCutout;
            var material = factory.Create(shader, source.Diffuse, source.Tint, cutout ? (source.AlphaBlend ? 1f / 255f : .35f) : 0, hasColors, wrapMode);
            if (material.HasProperty("_Fullbright")) material.SetFloat("_Fullbright", source.Fullbright ? 1 : 0);
            if (source.AlphaBlend)
            {
                material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_ZWrite", 0);
                material.SetFloat("_AlphaBlend", 1);
                material.SetFloat("_AlphaToMask", 0);
                material.renderQueue = (int)RenderQueue.Transparent;
                material.SetShaderPassEnabled("ShadowCaster", false);
            }
            return new MeshPartResources(appearance.Key + " part " + index, source, material, colors)
            {
                AnimationFrames = source.DiffuseFrames?.Select(frame => factory.Texture(frame, wrapMode)).ToArray(),
                MinimumFrameSeconds = source.MinimumFrameSeconds,
                MaximumFrameSeconds = source.MaximumFrameSeconds
            };
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
