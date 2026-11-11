using System;
using Swg.Client.Core.Assets.Formats.Trn;
using Swg.Client.Core.Assets.Formats.Tga;
using Swg.Client.Core.Assets.Tre;
using UnityEngine;

namespace Swg.Client.Core.World.Terrain.Affectors
{
    internal static class TerrainColorRampAffector
    {
        public static TerrainLayer.Affector Create(TrnColorRampRule definition, WorldTerrain terrain, TreAssetLoader assets)
        {
            var fractal = terrain.GetFractal(definition.FractalId);
            int operation = definition.Operation;
            var image = TgaFile.Parse(assets.Read(definition.ImagePath));
            return (float x, float z, float weight, ref TerrainSample sample) =>
            {
                int index = Math.Max(0, Math.Min(image.Width - 1, (int)(fractal.Sample(x, z) * (image.Width - 1)))) * 4;
                sample.Color = new Color32(Color(sample.Color.r, image.Pixels[index], operation, weight), Color(sample.Color.g, image.Pixels[index + 1], operation, weight), Color(sample.Color.b, image.Pixels[index + 2], operation, weight), 255);
            };
        }

        static byte Color(byte previous, byte desired, int operation, float amount)
        {
            float target;
            switch (operation)
            {
                case 1:
                    target = Math.Min(255, previous + (int)(desired * amount));
                    break;
                case 2:
                    target = Math.Max(0, previous - (int)(desired * amount));
                    break;
                case 3:
                    target = amount * (.5f * desired + .5f * previous) + (1 - amount) * previous;
                    break;
                default:
                    target = amount * desired + (1 - amount) * previous;
                    break;
            }

            return (byte)target;
        }
    }
}
