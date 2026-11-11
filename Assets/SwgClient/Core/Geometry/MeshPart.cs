using UnityEngine;
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
        public byte[] VertexColors;
        public bool UseCharacterShader;
    }
}
