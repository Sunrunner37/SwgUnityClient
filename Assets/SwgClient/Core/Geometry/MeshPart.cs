using UnityEngine;
using Swg.Client.Core.Characters.Animation;
using Swg.Client.Core.Assets.Formats.Dds;

namespace Swg.Client.Core.Geometry
{
    public sealed class MeshPart
    {
        public Vector3[] Vertices;
        public Vector3[] Normals;
        public Vector2[] Uvs;
        public int[] Triangles;
        public TextureData Diffuse;
        public byte[] Tint;
        public bool AlphaCutout;
        public bool AlphaBlend;
        public bool Fullbright;
        public Color Emission;
        public bool VertexColorAmbientOnly;
        public int? PortalCellNumber;
        public bool InteriorDecoration;
        public TextureData[] DiffuseFrames;
        public float MinimumFrameSeconds;
        public float MaximumFrameSeconds;
        public byte[] VertexColors;
        public bool UseCharacterShader;
        public SkeletalMeshData Skinning;
    }
}
