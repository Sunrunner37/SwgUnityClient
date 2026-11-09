using UnityEngine;
using Swg.Client.Core.Assets.Formats.Dds;
using Swg.Client.Core.Assets.Tre;

namespace Swg.Client.Runtime.Assets.Tre
{
    public abstract class TreVisual : ScriptableObject
    {
        public abstract TextureData Load(TreAssetLoader loader);
        protected static int[] Tint(Color color)
        {
            Color32 c = color;
            return new[]
            {
                (int)c.r,
                (int)c.g,
                (int)c.b
            };
        }
    }
}
