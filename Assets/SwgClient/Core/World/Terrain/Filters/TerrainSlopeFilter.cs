using UnityEngine;

namespace Swg.Client.Core.World.Terrain.Filters
{
    public sealed class TerrainSlopeFilter : TerrainFilter
    {
        readonly float minimumNormalY;
        readonly float maximumNormalY;
        public override bool NeedsNormals => true;
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
            minimumNormalY = Mathf.Sin((90 - Mathf.Clamp(maximumDegrees, 0, 90)) * Mathf.Deg2Rad);
            maximumNormalY = Mathf.Sin((90 - Mathf.Clamp(minimumDegrees, 0, 90)) * Mathf.Deg2Rad);
        }

        // Native FSLP feathers the normal Y component, rather than interpolating angles in degrees.
        public override float Sample(float x, float z, in TerrainSample sample, float normalY = 1) => TerrainFilterRange.Sample(normalY, minimumNormalY, maximumNormalY, Feather, FeatherType);
    }
}
