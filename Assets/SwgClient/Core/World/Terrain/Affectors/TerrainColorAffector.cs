using System;
using Swg.Client.Core.Assets.Formats.Trn;
using Swg.Client.Core.Assets.Formats.Tga;
using Swg.Client.Core.Assets.Tre;
using UnityEngine;

namespace Swg.Client.Core.World.Terrain.Affectors
{
    internal static class TerrainColorAffector
    {
        public static TerrainLayer.Affector Create(TrnConstantColorRule rule) => (float x, float z, float weight, ref TerrainSample sample) => sample.Color = Apply(sample.Color, rule.Color, rule.Operation, weight);
        public static TerrainLayer.Affector Create(TrnHeightColorRampRule rule, TreAssetLoader assets)
        {
            var image = TgaFile.Parse(assets.Read(rule.ImagePath));
            return (float x, float z, float weight, ref TerrainSample sample) =>
            {
                if (sample.Height < rule.LowHeight || sample.Height > rule.HighHeight)
                    return;
                float range = rule.HighHeight - rule.LowHeight;
                int index = (range == 0 ? 0 : (int)((sample.Height - rule.LowHeight) / range * (image.Width - 1))) * 4;
                sample.Color = Apply(sample.Color, new Color32(image.Pixels[index], image.Pixels[index + 1], image.Pixels[index + 2], 255), rule.Operation, weight);
            };
        }

        static Color32 Apply(Color32 previous, Color32 desired, int operation, float amount) => new Color32(Channel(previous.r, desired.r, operation, amount), Channel(previous.g, desired.g, operation, amount), Channel(previous.b, desired.b, operation, amount), 255);
        public static byte Channel(byte previous, byte desired, int operation, float amount)
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
