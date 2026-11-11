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

        public override float Sample(float x, float z, float height) => TerrainFilterRange.Sample(height, Minimum, Maximum, Feather, FeatherType);
    }
}
