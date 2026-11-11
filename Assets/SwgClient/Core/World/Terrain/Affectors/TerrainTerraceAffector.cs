using Swg.Client.Core.Assets.Formats.Trn;

namespace Swg.Client.Core.World.Terrain.Affectors
{
    internal static class TerrainTerraceAffector
    {
        public static TerrainLayer.Affector Create(TrnTerraceRule definition)
        {
            float flat = definition.Value;
            float step = definition.Step;
            return (float x, float z, float weight, ref TerrainSample sample) =>
            {
                if (step <= 0)
                    return;
                float remainder = sample.Height % step;
                if (sample.Height == 0)
                    remainder += step;
                float target = sample.Height - remainder;
                float start = step * flat + target;
                if (sample.Height > start)
                    target += (sample.Height - start) / (step * (1 - flat)) * step;
                sample.Height += (target - sample.Height) * weight;
            };
        }
    }
}
