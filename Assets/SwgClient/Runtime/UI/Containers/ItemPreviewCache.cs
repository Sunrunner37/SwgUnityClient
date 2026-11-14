using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Swg.Client.Core.Assets.Appearances;
using Swg.Client.Core.World;
using Swg.Client.Runtime.Assets.Appearances;
using Swg.Client.Runtime.Assets.Shaders;
using Swg.Client.Runtime.World;
using Swg.Client.Runtime.World.Loading;
using UnityEngine;

namespace Swg.Client.Runtime.UI.Containers
{
    /// <summary>Game-lifetime resources shared by UI previews; callers decide which appearances to prepare and render.</summary>
    public sealed class ItemPreviewCache : IDisposable
    {
        public sealed class Preview
        {
            public AppearanceData Appearance { get; }
            public AppearanceResources Resources { get; }
            public Preview(AppearanceData appearance, AppearanceResources resources)
            {
                Appearance = appearance;
                Resources = resources;
            }
        }

        readonly Dictionary<string, Preview> appearances = new Dictionary<string, Preview>();
        readonly Dictionary<string, Task<AppearanceData>> pending = new Dictionary<string, Task<AppearanceData>>();
        readonly WorldContentLoader loader;
        readonly SwgMaterialFactory materials;
        readonly WorldRenderingContext rendering;

        public ItemPreviewCache(WorldContentLoader loader, SwgMaterialFactory materials, WorldRenderingContext rendering)
        {
            this.loader = loader;
            this.materials = materials;
            this.rendering = rendering;
        }

        public bool TryGet(string key, out Preview preview) => appearances.TryGetValue(key, out preview);

        // Run on Unity's main thread: reads run in parallel, GPU resources are created here.
        public IEnumerator Prepare(IEnumerable<AppearanceRequest> requests)
        {
            var reads = new Dictionary<string, Task<AppearanceData>>();
            foreach (var request in requests)
            {
                if (appearances.ContainsKey(request.Key)) continue;
                if (!pending.TryGetValue(request.Key, out var task))
                {
                    task = loader.LoadAppearanceAsync(request);
                    pending.Add(request.Key, task);
                }
                reads[request.Key] = task;
            }
            int prepared = 0;
            foreach (var entry in reads)
            {
                var read = entry.Value;
                yield return new WaitUntil(() => read.IsCompleted);
                // Another caller may have prepared the same appearance while we waited.
                if (appearances.ContainsKey(entry.Key)) continue;
                if (pending.TryGetValue(entry.Key, out var current) && current == read) pending.Remove(entry.Key);
                if (read.IsFaulted) Debug.LogException(read.Exception.GetBaseException());
                else if (!read.IsCanceled)
                {
                    var appearance = read.Result;
                    var resources = new AppearanceResources(appearance, materials, rendering.WorldShader, rendering.CharacterShader, rendering.ParticleShader);
                    appearances.Add(entry.Key, new Preview(appearance, resources));
                }
                if (++prepared % 4 == 0) yield return null;
            }
        }

        public void Dispose()
        {
            foreach (var preview in appearances.Values) preview.Resources.Dispose();
            appearances.Clear();
            pending.Clear();
        }
    }
}
