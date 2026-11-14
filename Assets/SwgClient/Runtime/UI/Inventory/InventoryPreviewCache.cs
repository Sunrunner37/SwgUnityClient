using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Swg.Client.Core;
using Swg.Client.Core.World.SceneObjects;
using Swg.Client.Runtime.Networking;
using Swg.Client.Runtime.UI.Containers;
using Swg.Client.Runtime.World.Loading;
using UnityEngine;

namespace Swg.Client.Runtime.UI.Inventory
{
    /// <summary>Game-lifetime preview resources; prepares inventory changes without rendering hidden UI.</summary>
    public sealed class InventoryPreviewCache : MonoBehaviour, IDisposable
    {
        readonly Dictionary<SwgId, ItemPreviewCache.Preview> objects = new Dictionary<SwgId, ItemPreviewCache.Preview>();
        ItemPreviewCache cache;
        ClientInventory inventory;
        WorldContentLoader loader;
        Coroutine warming;
        bool dirty;
        public bool IsReady => warming == null;
        public event Action Changed;

        public void Initialize(ClientInventory inventory, WorldContentLoader loader, ItemPreviewCache cache)
        {
            this.inventory = inventory;
            this.loader = loader;
            this.cache = cache;
            inventory.Changed += Refresh;
            Refresh();
        }

        public bool TryGet(SwgId swgId, out ItemPreviewCache.Preview preview) => objects.TryGetValue(swgId, out preview);

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
                yield return cache.Prepare(requests.Values);
                if (dirty)
                    continue;
                objects.Clear();
                foreach (var request in requests)
                    if (cache.TryGet(request.Value.Key, out var preview))
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
            objects.Clear();
            Destroy(gameObject);
        }
    }
}
