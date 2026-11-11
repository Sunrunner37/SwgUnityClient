using Swg.Client.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace Swg.Client.Runtime.World
{
    public sealed class WorldStreamer : IDisposable
    {
        readonly WorldContentLoader content;
        readonly WorldStreamingSettings settings;
        readonly WorldMeshLibrary meshes;
        readonly WorldTerrainRenderer terrainRenderer;
        readonly MonoBehaviour owner;
        readonly WorldSceneObjectRenderer entities;
        readonly Dictionary<Vector2Int, GameObject> terrain = new Dictionary<Vector2Int, GameObject>();
        readonly HashSet<SwgId> scenery = new HashSet<SwgId>();
        readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        Coroutine pending;
        bool disposed;
        public Vector3 ServerCenter { get; private set; }
        public int CompletedRegions { get; private set; }
        public float Progress { get; private set; }

        public WorldStreamer(WorldContentLoader content, WorldStreamingSettings settings, WorldMeshLibrary meshes, WorldTerrainRenderer terrainRenderer, WorldSceneObjectRenderer entities, MonoBehaviour owner)
        {
            this.content = content;
            this.settings = settings;
            this.meshes = meshes;
            this.terrainRenderer = terrainRenderer;
            this.owner = owner;
            this.entities = entities;
        }

        public IEnumerator LoadInitial(WorldRegionData initialRegion)
        {
            yield return Apply(initialRegion, lifetime.Token);
        }

        public void Update(Vector3 serverCenter)
        {
            var distance = new Vector2(serverCenter.x - ServerCenter.x, serverCenter.z - ServerCenter.z);
            if (distance.sqrMagnitude < settings.refreshDistance * settings.refreshDistance)
                return;
            pending = owner.StartCoroutine(Refresh(serverCenter, lifetime.Token));
        }

        IEnumerator Refresh(Vector3 serverCenter, CancellationToken token)
        {
            var task = content.LoadRegionAsync(serverCenter, settings, new HashSet<Vector2Int>(terrain.Keys), new HashSet<SwgId>(scenery), null, token);
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

        IEnumerator Apply(WorldRegionData region, CancellationToken token)
        {
            int count = 0;
            int total = Math.Max(1, region.Terrain.Count + region.Scenery.Count);

            foreach (var tile in region.Terrain)
            {
                try
                {
                    token.ThrowIfCancellationRequested();
                    terrain.Add(tile.Key, terrainRenderer.Create(tile.Value, owner.transform));
                    Progress = ++count / (float)total;
                }
                catch (Exception error)
                {
                    Debug.LogError(error.Message);
                }

                yield return null;
            }

            foreach (var sceneryModel in region.Scenery)
            {
                try
                {
                    token.ThrowIfCancellationRequested();
                    entities.ShowScenery(sceneryModel.Key, sceneryModel.Value);
                    scenery.Add(sceneryModel.Key);
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
                foreach (var key in terrain.Keys.Where(key => !region.TerrainKeys.Contains(key)).ToArray())
                {
                    terrainRenderer.Release(terrain[key]);
                    terrain.Remove(key);
                }

                foreach (SwgId swgId in scenery.Where(swgId => !region.ScenerySwgIds.Contains(swgId)).ToArray())
                {
                    entities.RemoveScenery(swgId);
                    scenery.Remove(swgId);
                }

                meshes.PruneModels(entities.ModelKeys);
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
