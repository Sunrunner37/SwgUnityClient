using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Swg.Client.Core;
using Swg.Client.Core.Assets.Appearances;
using Swg.Client.Core.World.SceneObjects;
using Swg.Client.Runtime.Assets.Appearances;
using Swg.Client.Runtime.Assets.Shaders;
using Swg.Client.Runtime.Networking;
using Swg.Client.Runtime.World;
using Swg.Client.Runtime.World.Loading;
using UnityEngine;

namespace Swg.Client.Runtime.UI.Inventory
{
    /// <summary>Game-lifetime preview resources; prepares inventory changes without rendering hidden UI.</summary>
    public sealed class InventoryPreviewCache : MonoBehaviour, IDisposable
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
        readonly Dictionary<SwgId, Preview> objects = new Dictionary<SwgId, Preview>();
        ClientInventory inventory;
        WorldContentLoader loader;
        SwgMaterialFactory materials;
        WorldRenderingContext rendering;
        Coroutine warming;
        bool dirty;
        public bool IsReady => warming == null;
        public event Action Changed;

        public void Initialize(ClientInventory inventory, WorldContentLoader loader, SwgMaterialFactory materials, WorldRenderingContext rendering)
        {
            this.inventory = inventory;
            this.loader = loader;
            this.materials = materials;
            this.rendering = rendering;
            inventory.Changed += Refresh;
            Refresh();
        }

        public bool TryGet(SwgId swgId, out Preview preview) => objects.TryGetValue(swgId, out preview);

        void Refresh()
        {
            dirty = true;
            if (warming == null)
                warming = StartCoroutine(Warm());
        }

        IEnumerator Warm()
        {
            // Coalesce replication bursts before capturing immutable appearance requests.
            yield return null;
            while (dirty)
            {
                dirty = false;
                var sceneObjects = inventory.Items.Cast<SceneObject>().ToList();
                if (inventory.Player != null)
                    sceneObjects.Add(inventory.Player);
                var requests = sceneObjects.ToDictionary(item => item.SwgId, loader.DescribeAppearance);
                var reads = new Dictionary<string, Task<AppearanceData>>();
                foreach (var request in requests.Values)
                    if (!appearances.ContainsKey(request.Key) && !reads.ContainsKey(request.Key))
                        reads.Add(request.Key, loader.LoadAppearanceAsync(request));
                int prepared = 0;
                foreach (var read in reads)
                {
                    yield return new WaitUntil(() => read.Value.IsCompleted);
                    if (read.Value.IsFaulted)
                        Debug.LogException(read.Value.Exception.GetBaseException(), this);
                    else if (!read.Value.IsCanceled)
                    {
                        var appearance = read.Value.Result;
                        var resources = new AppearanceResources(appearance, materials, rendering.WorldShader, rendering.CharacterShader, rendering.ParticleShader);
                        appearances.Add(read.Key, new Preview(appearance, resources));
                    }
                    if (++prepared % 4 == 0)
                        yield return null;
                }
                if (dirty)
                    continue;
                objects.Clear();
                foreach (var request in requests)
                    if (appearances.TryGetValue(request.Value.Key, out var preview))
                        objects.Add(request.Key, preview);
                Changed?.Invoke();
            }
            warming = null;
        }

        public void Dispose()
        {
            if (inventory != null)
                inventory.Changed -= Refresh;
            StopAllCoroutines();
            warming = null;
            foreach (var preview in appearances.Values)
                preview.Resources.Dispose();
            appearances.Clear();
            objects.Clear();
            Destroy(gameObject);
        }
    }
}
