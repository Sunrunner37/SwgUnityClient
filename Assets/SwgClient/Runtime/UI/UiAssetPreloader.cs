using System.Collections;
using System.Collections.Generic;
using Swg.Client.Runtime.Assets.Tre;
using UnityEngine;

namespace Swg.Client.Runtime.UI
{
    /// <summary>Prepares TRE artwork for this UI hierarchy and its dynamically instantiated prefabs without opening windows.</summary>
    public sealed class UiAssetPreloader : MonoBehaviour
    {
        public IEnumerator Prewarm()
        {
            var assets = GameServices.Resolve<TreRuntimeAssets>();
            if (!assets.Ready.IsCompleted)
                yield return new WaitUntil(() => assets.Ready.IsCompleted);
            if (assets.Ready.IsFaulted)
            {
                Debug.LogException(assets.Ready.Exception.GetBaseException(), this);
                yield break;
            }
            if (assets.Ready.IsCanceled)
                yield break;

            var bindings = GetComponentsInChildren<TreTextureBinding>(true);
            var visuals = new HashSet<TreVisual>();
            Collect(bindings, visuals);
            foreach (var component in GetComponentsInChildren<MonoBehaviour>(true))
                if (component is IUiPrefabSource source)
                    foreach (var prefab in source.UiPrefabs)
                        if (prefab != null)
                            Collect(prefab.GetComponentsInChildren<TreTextureBinding>(true), visuals);
            int prepared = 0;
            foreach (var visual in visuals)
            {
                try
                {
                    assets.LoadVisual(visual);
                }
                catch (System.Exception error)
                {
                    Debug.LogException(error, visual);
                }
                if (++prepared % 4 == 0)
                    yield return null;
            }
            // Apply to scene instances only; prefab assets remain untouched.
            foreach (var binding in bindings)
            {
                try
                {
                    binding.Apply(assets.LoadVisual);
                }
                catch (System.Exception error)
                {
                    Debug.LogException(error, binding);
                }
            }
        }

        static void Collect(TreTextureBinding[] bindings, HashSet<TreVisual> visuals)
        {
            foreach (var binding in bindings)
                if (binding.enabled && binding.asset != null)
                    visuals.Add(binding.asset);
        }
    }
}
