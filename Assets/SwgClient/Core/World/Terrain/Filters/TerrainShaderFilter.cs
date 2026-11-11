namespace Swg.Client.Core.World.Terrain.Filters
{
    public sealed class TerrainShaderFilter : TerrainFilter
    {
        public int FamilyId { get; }

        public TerrainShaderFilter(int familyId)
        {
            FamilyId = familyId;
        }

        // Preserve the existing Core3 height-evaluation fallback until shader sampling is supported.
        public override float Sample(float x, float z, float height) => 0;
    }
}
