namespace Swg.Client.Core.World.Terrain.Filters
{
    public sealed class TerrainFractalFilter : TerrainFilter
    {
        public int FractalId { get; }
        public NativeTerrainFractal Fractal { get; }
        public float Minimum { get; }
        public float Maximum { get; }
        public int FeatherType { get; }
        public float Feather { get; }
        public float Multiplier { get; }

        public TerrainFractalFilter(int fractalId, NativeTerrainFractal fractal, int featherType, float feather, float minimum, float maximum, float multiplier)
        {
            Minimum = minimum;
            Maximum = maximum;
            FeatherType = featherType;
            Feather = feather;
            FractalId = fractalId;
            Fractal = fractal;
            Multiplier = multiplier;
        }

        public override float Sample(float x, float z, in TerrainSample sample, float normalY = 1) => TerrainFilterRange.Sample(Fractal.Sample(x, z) * Multiplier, Minimum, Maximum, Feather, FeatherType);
    }
}
