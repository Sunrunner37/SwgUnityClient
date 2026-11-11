using Swg.Client.Runtime.Networking;
using Swg.Client.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Swg.Client.Core.Assets.Models;
using Swg.Client.Core.World;
using Swg.Client.Core.World.SceneObjects;
using UnityEngine;

namespace Swg.Client.Runtime.World
{
    /// <summary>Creates world visuals and loads appearances for explicitly changed objects.</summary>
    public sealed class WorldSceneObjectRenderer : MonoBehaviour
    {
        readonly Dictionary<SwgId, WorldAppearance> appearances = new Dictionary<SwgId, WorldAppearance>();
        readonly Dictionary<SwgId, string> loadedKeys = new Dictionary<SwgId, string>();
        readonly Dictionary<SwgId, ModelData> models = new Dictionary<SwgId, ModelData>();
        readonly HashSet<SwgId> dirtyAppearances = new HashSet<SwgId>();
        readonly HashSet<SwgId> pendingModels = new HashSet<SwgId>();
        readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        WorldState data;
        WorldContentLoader content;
        GameReplication source;
        SceneObjectRegistry registry;
        WorldMeshLibrary meshes;
        int uploadsPerFrame;
        bool loading;
        public IEnumerable<string> ModelKeys => models.Values.Select(model => model.Key);

        public void Initialize(WorldState sceneData, WorldContentLoader content, WorldLoadingData loading, GameReplication world, WorldMeshLibrary meshLibrary, int uploadBudget)
        {
            data = sceneData;
            this.content = content;
            source = world;
            registry = world.SceneObjects;
            meshes = meshLibrary;
            uploadsPerFrame = uploadBudget;
            registry.Removed += OnObjectRemoved;
            registry.ObjectChanged += OnObjectChanged;
            // A single initial pass; subsequent updates identify their affected object IDs.
            foreach (var state in source.CaptureSnapshot().Objects)
                RefreshAppearance(state.SwgId);
            foreach (var pair in loading.InitialModels)
                if (registry.TryGet<SpatialSceneObject>(pair.Key, out var state) && appearances.TryGetValue(pair.Key, out var appearance) && pair.Value.Key == appearance.Key)
                {
                    Show(state, pair.Value);
                    loadedKeys[pair.Key] = appearance.Key;
                    pendingModels.Remove(pair.Key);
                }

            loading.InitialModels.Clear();
        }

        public void ShowScenery(SwgId swgId, ModelData model)
        {
            if (registry.TryGet<SpatialSceneObject>(swgId, out var state))
                Show(state, model);
        }

        public void RemoveScenery(SwgId swgId) => RemoveInstance(swgId);
        void OnObjectChanged(SceneObject state, SceneObjectChange changes)
        {
            if ((changes & (SceneObjectChange.Appearance | SceneObjectChange.Containment)) != 0)
                dirtyAppearances.Add(state.SwgId);
        }

        void LateUpdate()
        {
            // Coalesce multiple baseline/delta packets for the same object within this frame.
            foreach (var swgId in dirtyAppearances)
                RefreshAppearance(swgId);
            dirtyAppearances.Clear();
            if (!loading && pendingModels.Count > 0)
                StartCoroutine(LoadPending());
        }

        void RefreshAppearance(SwgId swgId)
        {
            registry.TryGet(swgId, out var state);
            var appearance = state == null ? null : WorldAppearance.Describe(data, state);
            if (appearance == null)
            {
                // Scenery lifetime is managed by the streamer, not replicated appearance changes.
                if (appearances.Remove(swgId))
                    RemoveInstance(swgId);
                loadedKeys.Remove(swgId);
                pendingModels.Remove(swgId);
                return;
            }

            appearances[swgId] = appearance;
            if (ManagedSceneObject.TryGetObjectFrom(swgId, out var instance))
                instance.name = string.IsNullOrEmpty(state.Name) ? appearance.TemplatePath : state.Name;
            if (!loadedKeys.TryGetValue(swgId, out var key) || key != appearance.Key)
                pendingModels.Add(swgId);
            else
                pendingModels.Remove(swgId);
        }

        IEnumerator LoadPending()
        {
            loading = true;
            try
            {
                var requested = pendingModels.ToDictionary(swgId => swgId, swgId => appearances[swgId]);
                pendingModels.Clear();
                // Detached snapshots are needed only for asset work/cache writes, never movement.
                var task = content.LoadModelsAsync(source.CaptureSnapshot(), requested, lifetime.Token);
                _ = task.ContinueWith(completed =>
                {
                    _ = completed.Exception;
                }, System.Threading.Tasks.TaskContinuationOptions.OnlyOnFaulted | System.Threading.Tasks.TaskContinuationOptions.ExecuteSynchronously);
                yield return new WaitUntil(() => task.IsCompleted);
                if (task.IsCanceled)
                    yield break;
                if (task.IsFaulted)
                {
                    Debug.LogException(task.Exception.GetBaseException(), this);
                    yield break;
                }

                int count = 0;
                foreach (var request in requested)
                {
                    // Changes can arrive while decoding or between frame-budgeted uploads.
                    if (dirtyAppearances.Remove(request.Key))
                        RefreshAppearance(request.Key);
                    if (!registry.TryGet<SpatialSceneObject>(request.Key, out var state) || !appearances.TryGetValue(request.Key, out var current) || current.Key != request.Value.Key)
                        continue;
                    loadedKeys[request.Key] = request.Value.Key;
                    pendingModels.Remove(request.Key);
                    if (task.Result.TryGetValue(request.Key, out var model))
                    {
                        try
                        {
                            Show(state, model);
                        }
                        catch (Exception error)
                        {
                            Debug.LogException(error, this);
                        }
                    }
                    else
                        RemoveInstance(request.Key);
                    if (++count % uploadsPerFrame == 0)
                        yield return null;
                }
            }
            finally
            {
                loading = false;
            }
        }

        void Show(SpatialSceneObject state, ModelData model)
        {
            var instance = meshes.Create(model, transform);
            instance.name = string.IsNullOrEmpty(state.Name) ? state.TemplatePath : state.Name;
            var serverWorldPose = registry.GetServerWorldPose(state.SwgId);
            instance.transform.localPosition = serverWorldPose.position - data.RenderOrigin;
            instance.transform.localRotation = serverWorldPose.rotation;
            instance.transform.localScale = Vector3.one * state.Scale;
            // Preserve the displayed pose if an appearance finishes loading during interpolation.
            if (ManagedSceneObject.TryGetObjectFrom(state.SwgId, out var previous))
            {
                instance.transform.localPosition = previous.transform.localPosition;
                instance.transform.localRotation = previous.transform.localRotation;
            }

            RemoveInstance(state.SwgId);
            var managed = instance.AddComponent<ManagedSceneObject>();
            managed.Bind(state.SwgId);
            instance.AddComponent<SceneObjectMovement>().Initialize(state);
            models[state.SwgId] = model;
        }

        void RemoveInstance(SwgId swgId)
        {
            if (ManagedSceneObject.TryGetObjectFrom(swgId, out var instance))
            {
                instance.GetComponent<SceneObjectMovement>()?.Unbind();
                instance.GetComponent<ManagedSceneObject>().Unbind();
                instance.SetActive(false);
                Destroy(instance);
            }

            models.Remove(swgId);
        }

        void OnObjectRemoved(SceneObject state)
        {
            RemoveInstance(state.SwgId);
            appearances.Remove(state.SwgId);
            loadedKeys.Remove(state.SwgId);
            dirtyAppearances.Remove(state.SwgId);
            pendingModels.Remove(state.SwgId);
        }

        void OnDestroy()
        {
            lifetime.Cancel();
            lifetime.Dispose();
            if (registry == null)
                return;
            registry.Removed -= OnObjectRemoved;
            registry.ObjectChanged -= OnObjectChanged;
            foreach (var swgId in models.Keys.ToArray())
                RemoveInstance(swgId);
        }
    }
}
