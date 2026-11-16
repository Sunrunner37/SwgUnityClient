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
using Stopwatch = System.Diagnostics.Stopwatch;

namespace Swg.Client.Runtime.World.Streaming
{
    public sealed class WorldStreamer : IDisposable
    {
        readonly WorldContentLoader content;
        readonly WorldRenderingContext context;
        WorldStreamingSettings settings => context.Streaming;
        readonly WorldAppearanceLibrary appearanceLibrary;
        readonly WorldTerrainRenderer terrainRenderer;
        WorldFloraRenderer flora => context.FloraRenderer;
        MonoBehaviour owner => context.Owner;
        WorldSceneObjectRenderer entities => context.Renderer;
        readonly HashSet<SwgId> scenery = new HashSet<SwgId>();
        CancellationTokenSource lifetime = new CancellationTokenSource();
        Coroutine pending;
        bool disposed;
        public Vector3 ServerCenter { get; private set; }
        public int CompletedRegions { get; private set; }
        public float Progress { get; private set; }

        public WorldStreamer(WorldContentLoader content, WorldRenderingContext context, WorldAppearanceLibrary appearanceLibrary, WorldTerrainRenderer terrainRenderer)
        {
            this.content = content;
            this.context = context;
            this.appearanceLibrary = appearanceLibrary;
            this.terrainRenderer = terrainRenderer;
        }

        public IEnumerator LoadInitial(WorldRegionData initialRegion)
        {
            yield return Apply(initialRegion, lifetime.Token, true);
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
                var task = content.LoadRegionAsync(serverCenter, settings, new HashSet<Vector2Int>(terrainRenderer.Keys), new HashSet<SwgId>(scenery), null, token, terrainRenderer.Resolutions, flora.Details);
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

        IEnumerator Apply(WorldRegionData region, CancellationToken token, bool initial = false)
        {
            var timer = Stopwatch.StartNew();
            double activeMilliseconds = 0;
            double sliceStart = Time.realtimeSinceStartupAsDouble;
            float budget = initial ? settings.initialUploadBudgetMilliseconds : settings.uploadBudgetMilliseconds;
            int uploadLimit = initial ? 32 : settings.uploadsPerFrame;
            int count = 0;
            int uploads = 0;
            double deadline = Time.realtimeSinceStartupAsDouble + budget * .001;
            int total = Math.Max(1, region.Terrain.Count + region.Flora.Count + region.Scenery.Count);
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

                if (++uploads >= uploadLimit || Time.realtimeSinceStartupAsDouble >= deadline)
                {
                    activeMilliseconds += (Time.realtimeSinceStartupAsDouble - sliceStart) * 1000;
                    yield return null;
                    uploads = 0;
                    sliceStart = Time.realtimeSinceStartupAsDouble;
                    deadline = sliceStart + budget * .001;
                }
            }

            foreach (var tile in region.Flora)
            {
                token.ThrowIfCancellationRequested();
                flora.AddOrReplace(tile.Key, tile.Value);
                Progress = ++count / (float)total;
                if (++uploads >= uploadLimit || Time.realtimeSinceStartupAsDouble >= deadline)
                {
                    activeMilliseconds += (Time.realtimeSinceStartupAsDouble - sliceStart) * 1000;
                    yield return null;
                    uploads = 0;
                    sliceStart = Time.realtimeSinceStartupAsDouble;
                    deadline = sliceStart + budget * .001;
                }
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

                if (++uploads >= uploadLimit || Time.realtimeSinceStartupAsDouble >= deadline)
                {
                    activeMilliseconds += (Time.realtimeSinceStartupAsDouble - sliceStart) * 1000;
                    yield return null;
                    uploads = 0;
                    sliceStart = Time.realtimeSinceStartupAsDouble;
                    deadline = sliceStart + budget * .001;
                }
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
                activeMilliseconds += (Time.realtimeSinceStartupAsDouble - sliceStart) * 1000;
                Debug.Log($"World {(initial ? "initial" : "streamed")} uploads: {timer.Elapsed.TotalSeconds:F2}s wall, {activeMilliseconds:F0}ms active; {region.Terrain.Count} terrain / {region.Flora.Count} flora / {region.Scenery.Count} scenery");
            }
            catch (Exception error)
            {
                Debug.LogError(error.Message);
            }
        }

        public void Reset()
        {
            lifetime.Cancel();
            if (pending != null && owner != null) owner.StopCoroutine(pending);
            pending = null;
            lifetime.Dispose();
            lifetime = new CancellationTokenSource();
            scenery.Clear();
            terrainRenderer.Clear();
            Progress = 0;
            CompletedRegions = 0;
            ServerCenter = default;
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
