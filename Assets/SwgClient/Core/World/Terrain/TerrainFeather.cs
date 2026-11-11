using System;

namespace Swg.Client.Core.World.Terrain
{
    internal static class TerrainFeather
    {
        public static float Apply(float value, int type) => type switch
        {
            0 => value,
            1 => value * value,
            2 => (float)Math.Sqrt(value),
            3 => value * value * (3 - 2 * value),
            _ => 0
        };
    }
}
