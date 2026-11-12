namespace Swg.Client.Core.World.Terrain.Filters
{
    public sealed class TerrainHeightFilter : TerrainFilter
    {
        public float Minimum { get; }
        public float Maximum { get; }
        public int FeatherType { get; }
        public float Feather { get; }

        public TerrainHeightFilter(float minimum, float maximum, int featherType, float feather)
        {
            Minimum = minimum;
            Maximum = maximum;
            FeatherType = featherType;
            Feather = feather;
        }

        public override float Sample(float x, float z, in TerrainSample sample, float normalY = 1) => TerrainFilterRange.Sample(sample.Height, Minimum, Maximum, Feather, FeatherType);
    }
}
