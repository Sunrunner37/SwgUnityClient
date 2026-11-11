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
                sample.Color = new Color32(TerrainColorAffector.Channel(sample.Color.r, image.Pixels[index], operation, weight), TerrainColorAffector.Channel(sample.Color.g, image.Pixels[index + 1], operation, weight), TerrainColorAffector.Channel(sample.Color.b, image.Pixels[index + 2], operation, weight), 255);
            };
        }
    }
}
