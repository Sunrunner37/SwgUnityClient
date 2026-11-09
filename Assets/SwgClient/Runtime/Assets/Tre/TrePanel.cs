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
    public sealed class TreStyleLayer
    {
        [Tooltip("Style Name from the SWG UI markup, e.g. fullFrame or whiteOutline_palW.")]
        public string style = "fullFrame";
        public string state = "default";
        [Tooltip("Destination rectangle in top-left pixels.")]
        public RectInt destination;
        public Color tint = Color.white;
    }

    [CreateAssetMenu(menuName = "SWG/Panel", fileName = "TrePanel")]
    public sealed class TrePanel : TreVisual
    {
        public string atlasPath = "texture/ui_rebel_final.dds";
        public string stylesPath = "ui/ui_styles.inc";
        public Vector2Int size = new Vector2Int(336, 201);
        public List<TreStyleLayer> layers = new List<TreStyleLayer>();
        public override TextureData Load(TreAssetLoader loader)
        {
            if (size.x <= 0 || size.y <= 0 || size.x > 8192 || size.y > 8192)
                throw new InvalidDataException("Panel size must be 1..8192 pixels.");
            var result = new TextureData(size.x, size.y);
            var composer = new UiCompositor(loader, atlasPath, stylesPath);
            foreach (var layer in layers)
            {
                var r = layer.destination;
                composer.Nine(result, layer.style, r.x, r.y, r.width, r.height, Tint(layer.tint), layer.tint.a, layer.state);
            }

            return result;
        }
    }
}
