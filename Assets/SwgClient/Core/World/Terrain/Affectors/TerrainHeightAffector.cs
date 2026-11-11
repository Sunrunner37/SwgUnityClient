using System;
using Swg.Client.Core.Assets.Formats.Trn;

namespace Swg.Client.Core.World.Terrain.Affectors
{
    internal static class TerrainHeightAffector
    {
        public static TerrainLayer.Affector Create(TrnHeightRule definition, WorldTerrain terrain)
        {
            NativeTerrainFractal heightFractal = definition.Kind == "AHFR" ? terrain.GetFractal(definition.FractalId) : null;
            int heightOperation = definition.Operation;
            float value = definition.Value;
            return (float x, float z, float weight, ref TerrainSample sample) =>
            {
                float target = heightFractal == null ? value : value * heightFractal.Sample(x, z);
                switch (heightOperation)
                {
                    case 1:
                        sample.Height += target * weight;
                        break;
                    case 2:
                        sample.Height -= target * weight;
                        break;
                    case 3:
                        sample.Height += (sample.Height * target - sample.Height) * weight;
                        break;
                    case 4:
                        if (heightFractal == null)
                            sample.Height = 0;
                        break;
                    default:
                        sample.Height += (target - sample.Height) * weight;
                        break;
                }
            };
        }
    }
}
