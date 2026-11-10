using Swg.Client.Runtime.Assets.Tre;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Swg.Client.Runtime.Assets.Tre
{
    // Each binding loads independently when enabled; editor preview may also apply textures directly.
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class TreTextureBinding : MonoBehaviour
    {
        public TreVisual asset;
        public Vector4 spriteBorder;
        Sprite ownedSprite;
        Coroutine binding;
        Graphic graphic;
        Renderer targetRenderer;
        bool showGraphic;
        bool showRenderer;
        bool visibilityHidden;
        public bool IsBound { get; private set; }
        public Exception Failure { get; private set; }

        public static void ThrowIfFailed(Transform root)
        {
            foreach (var binding in root.GetComponentsInChildren<TreTextureBinding>(true))
                if (binding.isActiveAndEnabled && binding.Failure != null)
                    throw binding.Failure;
        }

        void OnEnable()
        {
            if (Application.IsPlaying(gameObject) && asset != null)
                binding = StartCoroutine(BindRuntime());
        }

        IEnumerator BindRuntime()
        {
            Failure = null;
            graphic = GetComponent<Graphic>();
            targetRenderer = GetComponent<Renderer>();
            showGraphic = graphic != null && graphic.enabled;
            showRenderer = targetRenderer != null && targetRenderer.enabled;
            visibilityHidden = true;
            if (graphic != null)
                graphic.enabled = false;
            if (targetRenderer != null)
                targetRenderer.enabled = false;
            try
            {
                var assets = GameServices.Resolve<TreRuntimeAssets>();
                yield return new WaitUntil(() => assets.Ready.IsCompleted);
                if (assets.Ready.IsFaulted)
                {
                    Failure = assets.Ready.Exception.GetBaseException();
                    Debug.LogException(Failure, this);
                    yield break;
                }

                if (assets.Ready.IsCanceled)
                    yield break;
                try
                {
                    Apply(assets.LoadVisual);
                    IsBound = true;
                }
                catch (Exception error)
                {
                    Failure = error;
                    Debug.LogException(error, this);
                }
            }
            finally
            {
                RestoreVisibility();
            }
        }

        void OnDisable()
        {
            if (binding != null)
                StopCoroutine(binding);
            binding = null;
            RestoreVisibility();
            IsBound = false;
        }

        void RestoreVisibility()
        {
            if (!visibilityHidden)
                return;
            visibilityHidden = false;
            if (graphic != null)
                graphic.enabled = showGraphic;
            if (targetRenderer != null)
                targetRenderer.enabled = showRenderer;
        }

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
