using Swg.Client.Core.Assets.Formats.Iff;
using Swg.Client.Core.Geometry;

namespace Swg.Client.Core.Characters.Models
{
    public sealed class CachedMeshPartSource
    {
        public MeshPart Part;
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
