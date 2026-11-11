namespace Swg.Client.Core.World.Terrain.Filters
{
    public sealed class TerrainSlopeFilter : TerrainFilter
    {
        public float MinimumDegrees { get; }
        public float MaximumDegrees { get; }
        public int FeatherType { get; }
        public float Feather { get; }

        public TerrainSlopeFilter(float minimumDegrees, float maximumDegrees, int featherType, float feather)
        {
            MinimumDegrees = minimumDegrees;
            MaximumDegrees = maximumDegrees;
            FeatherType = featherType;
            Feather = feather;
        }

        // Preserve the existing Core3 height-evaluation fallback until normal sampling is supported.
        public override float Sample(float x, float z, float height) => 0;
    }
}
