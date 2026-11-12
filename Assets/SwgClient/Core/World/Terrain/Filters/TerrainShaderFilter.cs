namespace Swg.Client.Core.World.Terrain.Filters
{
    public sealed class TerrainShaderFilter : TerrainFilter
    {
        public int FamilyId { get; }

        public TerrainShaderFilter(int familyId)
        {
            FamilyId = familyId;
        }

        public override float Sample(float x, float z, in TerrainSample sample, float normalY = 1) => sample.ShaderFamily == FamilyId ? 1 : 0;
    }
}
