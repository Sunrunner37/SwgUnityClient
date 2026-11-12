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
        // Nested random subsets avoid distant rows while keeping positions stable when tiles refine.
        public int DetailAt(int x, int z)
        {
            var random = new TerrainRandom(x + (Seed & 0xffff) + 173, z + (Seed >> 16) + 947);
            float rank = random.Next();
            return rank < .0625f ? 2 : rank < .25f ? 1 : 0;
        }

        public Vector2 PositionAt(int x, int z)
        {
            float border = Spacing - 2 * Border > 0 ? Border : 0;
            var random = new TerrainRandom(x + Seed, z);
            float range = Spacing - 2 * border;
            return new Vector2(x * Spacing + border + random.Next() * range, z * Spacing + border + random.Next() * range);
        }
    }
}
