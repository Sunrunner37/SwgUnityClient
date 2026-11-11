using System;

namespace Swg.Client.Core.World.Terrain.Filters
{
    internal static class TerrainFilterRange
    {
        public static float Sample(float value, float minimum, float maximum, float feather, int featherType)
        {
            if (value <= minimum || value >= maximum)
                return 0;
            float width = (maximum - minimum) * feather * .5f;
            return TerrainFeather.Apply(width <= 0 ? 1 : Math.Min(1, Math.Min(value - minimum, maximum - value) / width), featherType);
        }
    }
}
