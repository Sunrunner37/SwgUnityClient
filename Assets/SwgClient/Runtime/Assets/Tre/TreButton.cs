using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Swg.Client.Core.Assets.Formats.Dds;
using Swg.Client.Core.Assets.Tre;
using Swg.Client.Runtime.UI;

namespace Swg.Client.Runtime.Assets.Tre
{
    [Serializable]
    public sealed class TreAtlasPatch
    {
        [Tooltip("Source endpoints x1,y1,x2,y2 in top-left pixels. Reverse endpoints to mirror.")]
        public Vector2Int sourceStart;
        public Vector2Int sourceEnd;
        public RectInt destination;
        public Color tint = Color.white;
    }

    [CreateAssetMenu(menuName = "SWG/Button", fileName = "TreButton")]
    public sealed class TreButton : TreVisual
    {
        public string atlasPath = "texture/ui_rebel_final.dds";
        public string stylesPath = "ui/ui_styles.inc";
        public string frameStyle = "nextFrame";
        public string frameState = "default";
        public Vector2Int size = new Vector2Int(158, 50);
        public Color frameTint = Color.white;
        public List<TreAtlasPatch> patches = new List<TreAtlasPatch>();
        public override TextureData Load(TreAssetLoader loader)
        {
            if (size.x <= 0 || size.y <= 0 || size.x > 8192 || size.y > 8192)
                throw new InvalidDataException("Button size must be 1..8192 pixels.");
            var result = new TextureData(size.x, size.y);
            var composer = new UiCompositor(loader, atlasPath, stylesPath);
            composer.Nine(result, frameStyle, 0, 0, size.x, size.y, Tint(frameTint), frameTint.a, frameState);
            foreach (var patch in patches)
            {
                var r = patch.destination;
                composer.Copy(result, new[] { patch.sourceStart.x, patch.sourceStart.y, patch.sourceEnd.x, patch.sourceEnd.y }, new[] { r.x, r.y, r.width, r.height }, Tint(patch.tint), patch.tint.a);
            }

            return result;
        }
    }
}
