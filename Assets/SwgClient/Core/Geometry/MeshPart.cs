using Swg.Client.Core.Assets.Formats.Dds;

namespace Swg.Client.Core.Geometry
{
    public sealed class MeshPart
    {
        public Float3[] Vertices;
        public Float3[] Normals;
        public Float2[] Uvs;
        public int[] Triangles;
        public TextureData Diffuse;
        public byte[] Tint;
    }
}
