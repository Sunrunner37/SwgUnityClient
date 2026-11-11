using Swg.Client.Runtime.World.Loading;
using Swg.Client.Runtime.World.SceneObjects;
using Swg.Client.Runtime.World.Terrain;
using Swg.Client.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace Swg.Client.Runtime.World.Streaming
{
    public sealed class WorldStreamer : IDisposable
    {
        readonly WorldContentLoader content;
        readonly WorldStreamingSettings settings;
        readonly WorldAppearanceLibrary appearanceLibrary;
        readonly WorldTerrainRenderer terrainRenderer;
        readonly WorldFloraRenderer flora;
        readonly MonoBehaviour owner;
        readonly WorldSceneObjectRenderer entities;
        readonly HashSet<SwgId> scenery = new HashSet<SwgId>();
        readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        Coroutine pending;
        bool disposed;
        public Vector3 ServerCenter { get; private set; }
        public int CompletedRegions { get; private set; }
        public float Progress { get; private set; }

        public WorldStreamer(WorldContentLoader content, WorldStreamingSettings settings, WorldAppearanceLibrary appearanceLibrary, WorldTerrainRenderer terrainRenderer, WorldSceneObjectRenderer entities, MonoBehaviour owner, WorldFloraRenderer flora)
        {
            this.content = content;
            this.settings = settings;
            this.appearanceLibrary = appearanceLibrary;
            this.terrainRenderer = terrainRenderer;
            this.owner = owner;
            this.flora = flora;
            this.entities = entities;
        }

        public IEnumerator LoadInitial(WorldRegionData initialRegion)
        {
            yield return Apply(initialRegion, lifetime.Token);
        }

        public void Update(Vector3 serverCenter)
        {
            if (disposed || pending != null)
                return;
            var distance = new Vector2(serverCenter.x - ServerCenter.x, serverCenter.z - ServerCenter.z);
            if (distance.sqrMagnitude < settings.refreshDistance * settings.refreshDistance)
                return;
            pending = owner.StartCoroutine(Refresh(serverCenter, lifetime.Token));
        }

        IEnumerator Refresh(Vector3 serverCenter, CancellationToken token)
        {
            try
            {
                var task = content.LoadRegionAsync(serverCenter, settings, new HashSet<Vector2Int>(terrainRenderer.Keys), new HashSet<SwgId>(scenery), null, token, new HashSet<Vector2Int>(flora.Keys), terrainRenderer.Resolutions);
                _ = task.ContinueWith(completed =>
                {
                    _ = completed.Exception;
                }, System.Threading.Tasks.TaskContinuationOptions.OnlyOnFaulted | System.Threading.Tasks.TaskContinuationOptions.ExecuteSynchronously);
                yield return new WaitUntil(() => task.IsCompleted);
                if (task.IsFaulted)
                {
                    Debug.LogError(task.Exception.GetBaseException().Message);
                    yield break;
                }

                yield return Apply(task.Result, token);
            }
            finally
            {
                pending = null;
            }
        }

        IEnumerator Apply(WorldRegionData region, CancellationToken token)
        {
            int count = 0;
            int total = Math.Max(1, region.Terrain.Count + region.Scenery.Count);
            foreach (var tile in region.Terrain)
            {
                try
                {
                    token.ThrowIfCancellationRequested();
                    terrainRenderer.AddOrReplace(tile.Key, tile.Value, owner.transform);
                    Progress = ++count / (float)total;
                }
                catch (Exception error)
                {
                    Debug.LogError(error.Message);
                }

                yield return null;
            }

            foreach (var tile in region.Flora)
            {
                token.ThrowIfCancellationRequested();
                flora.Add(tile.Key, tile.Value);
                yield return null;
            }

            flora.Retain(region.FloraKeys);
            foreach (var sceneryAppearance in region.Scenery)
            {
                try
                {
                    token.ThrowIfCancellationRequested();
                    entities.ShowScenery(sceneryAppearance.Key, sceneryAppearance.Value);
                    scenery.Add(sceneryAppearance.Key);
                    Progress = ++count / (float)total;
                }
                catch (Exception error)
                {
                    Debug.LogError(error.Message);
                }

                if (count % settings.uploadsPerFrame == 0)
                    yield return null;
            }

            try
            {
                token.ThrowIfCancellationRequested();
                terrainRenderer.Retain(region.TerrainKeys);
                foreach (SwgId swgId in scenery.Where(swgId => !region.ScenerySwgIds.Contains(swgId)).ToArray())
                {
                    entities.RemoveScenery(swgId);
                    scenery.Remove(swgId);
                }

                appearanceLibrary.PruneAppearances(entities.AppearanceKeys);
                ServerCenter = region.ServerCenter;
                Progress = 1;
                CompletedRegions++;
            }
            catch (Exception error)
            {
                Debug.LogError(error.Message);
            }
        }

        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            lifetime.Cancel();
            if (pending != null && owner != null)
                owner.StopCoroutine(pending);
            lifetime.Dispose();
        }
    }
}
