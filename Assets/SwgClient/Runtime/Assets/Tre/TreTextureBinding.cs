using System;
using UnityEngine;
using UnityEngine.UI;

namespace Swg.Client.Runtime.Assets.Tre
{
    // A rendering target only: each asset type owns its own loading/composition settings.
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class TreTextureBinding : MonoBehaviour
    {
        public TreVisual asset;
        public Vector4 spriteBorder;
        Sprite ownedSprite;
        void OnDestroy()
        {
            if (ownedSprite != null)
            {
                if (Application.isPlaying)
                    Destroy(ownedSprite);
                else
                    DestroyImmediate(ownedSprite);
            }
        }

        public string materialProperty = "_BaseMap";
        public void Apply(Func<TreVisual, Texture2D> load)
        {
            if (!enabled || asset == null)
                return;
            var texture = load(asset);
            var image = GetComponent<RawImage>();
            if (image != null)
                image.texture = texture;
            var sliced = GetComponent<Image>();
            if (sliced != null)
            {
                if (ownedSprite == null || ownedSprite.texture != texture || ownedSprite.border != spriteBorder)
                {
                    if (ownedSprite != null)
                    {
                        if (Application.isPlaying)
                            Destroy(ownedSprite);
                        else
                            DestroyImmediate(ownedSprite);
                    }

                    ownedSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect, spriteBorder);
                    ownedSprite.hideFlags = HideFlags.HideAndDontSave;
                }

                sliced.sprite = ownedSprite;
            }

            var renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                block.SetTexture(materialProperty, texture);
                renderer.SetPropertyBlock(block);
            }
        }
    }
}
