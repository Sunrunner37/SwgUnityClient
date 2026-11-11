using System;
using UnityEngine;

namespace Swg.Client.Core.World.Terrain
{
    /// <summary>Native TRN radial-manager spacing, placement border, seed and visibility range.</summary>
    public sealed class TerrainFloraDistribution
    {
        public float MinimumDistance;
        public float MaximumDistance;
        public float Spacing;
        public float Border;
        public uint Seed;
        public Vector2 PositionAt(int x, int z)
        {
            float border = Spacing - 2 * Border > 0 ? Border : 0;
            var random = new TerrainRandom(x + Seed, z);
            float range = Spacing - 2 * border;
            return new Vector2(x * Spacing + border + random.Next() * range, z * Spacing + border + random.Next() * range);
        }
    }
}
