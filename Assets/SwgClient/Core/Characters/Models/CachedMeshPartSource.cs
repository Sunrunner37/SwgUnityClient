using Swg.Client.Core.Assets.Formats.Iff;

namespace Swg.Client.Core.Characters.Models
{
    public sealed class CachedMeshPartSource
    {
        public string ObjectTemplate;
        public string MeshPath;
        public string ShaderPath;
        public int[] PositionIndices;
        public int[] TriangleOcclusionCombinations;
        // Preserves TWHD/TWDT/XFNM for a separately implemented skeletal pose pass.
        // POSN/NORM already contain the cached customization's sparse blend deltas.
        public IffNode MeshVersion;
    }
}
