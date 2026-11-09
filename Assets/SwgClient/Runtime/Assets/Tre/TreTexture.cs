using UnityEngine;
using Swg.Client.Core.Assets.Formats.Dds;
using Swg.Client.Core.Assets.Tre;

namespace Swg.Client.Runtime.Assets.Tre
{
    [CreateAssetMenu(menuName = "SWG/Texture", fileName = "TreTexture")]
    public sealed class TreTexture : TreVisual
    {
        [Tooltip("Virtual SWG path inside the mounted archives, e.g. texture/intro_logo.dds")]
        public string swgPath;
        public bool crop;
        [Tooltip("Pixels measured from the top-left of the original texture.")]
        public RectInt sourceRect;
        public Color tint = Color.white;
        public override TextureData Load(TreAssetLoader loader)
        {
            var texture = TextureData.Load(loader, swgPath);
            if (crop)
                texture = texture.Crop(sourceRect.x, sourceRect.y, sourceRect.width, sourceRect.height);
            Color32 color = tint;
            for (int p = 0; p < texture.Pixels.Length; p += 4)
            {
                texture.Pixels[p] = (byte)(texture.Pixels[p] * color.r / 255);
                texture.Pixels[p + 1] = (byte)(texture.Pixels[p + 1] * color.g / 255);
                texture.Pixels[p + 2] = (byte)(texture.Pixels[p + 2] * color.b / 255);
                texture.Pixels[p + 3] = (byte)(texture.Pixels[p + 3] * color.a / 255);
            }

            return texture;
        }
    }
}
