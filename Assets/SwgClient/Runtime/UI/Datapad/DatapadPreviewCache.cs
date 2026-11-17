using System;
using System.Collections;
using System.Linq;
using Swg.Client.Runtime.Networking;
using Swg.Client.Runtime.UI.Containers;
using Swg.Client.Runtime.World.Loading;
using UnityEngine;

namespace Swg.Client.Runtime.UI.Datapad
{
    /// <summary>Prepares replicated datapad contents during entry and on changes. Hidden UI never renders.</summary>
    public sealed class DatapadPreviewCache : MonoBehaviour, IDisposable
    {
        ClientDatapad datapad;
        ItemPreviewCache cache;
        WorldContentLoader loader;
        Coroutine warming;
        bool dirty;
        public bool IsReady { get; private set; }

        public void Initialize(ClientDatapad datapad, WorldContentLoader loader, ItemPreviewCache cache)
        {
            this.datapad = datapad;
            this.loader = loader;
            this.cache = cache;
            datapad.Changed += Refresh;
            Refresh();
        }

        void Refresh()
        {
            dirty = true;
            IsReady = false;
            if (warming == null) warming = StartCoroutine(Warm());
        }

        IEnumerator Warm()
        {
            yield return null; // Coalesce replication bursts.
            while (dirty)
            {
                yield return new WaitUntil(() => loader.HasWorldMetadata && datapad.Loading.IsCompleted);
                dirty = false;
                var entries = datapad.Entries(ClientDatapad.Tab.Waypoints)
                    .Concat(datapad.Entries(ClientDatapad.Tab.Data))
                    .Concat(datapad.Entries(ClientDatapad.Tab.PointsOfInterest)).ToArray();
                yield return Prepare(entries);
                if (dirty) continue;
                IsReady = true;
                // Hundreds of schematic models would dominate startup and memory. Warm the
                // initial page in the background; scrolling retains visible-row loading.
                yield return Prepare(datapad.Entries(ClientDatapad.Tab.DraftSchematics)
                    .OrderBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase).Take(64).ToArray());
            }
            warming = null;
        }

        IEnumerator Prepare(ClientDatapad.Entry[] entries)
        {
            // Bound background work so opening a window can prepare its visible rows promptly.
            for (int start = 0; start < entries.Length; start += 8)
            {
                yield return cache.Prepare(entries.Skip(start).Take(8).Select(entry => cache.Describe(entry.SceneObject, entry.PreviewPath)));
                yield return null;
            }
        }

        public void Dispose()
        {
            datapad.Changed -= Refresh;
            StopAllCoroutines();
            Destroy(gameObject);
        }
    }
}
