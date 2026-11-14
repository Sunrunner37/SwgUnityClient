using Swg.Client.Runtime.Assets.Appearances;
using Swg.Client.Runtime.World.Loading;
using Swg.Client.Runtime.World.Terrain;
using Swg.Client.Runtime.Networking;
using Swg.Client.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Swg.Client.Core.Assets.Appearances;
using Swg.Client.Core.World;
using Swg.Client.Core.World.SceneObjects;
using UnityEngine;
using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;

namespace Swg.Client.Runtime.World.SceneObjects
{
    /// <summary>Creates world visuals and loads appearances for explicitly changed objects.</summary>
    public sealed class WorldSceneObjectRenderer : MonoBehaviour
    {
        readonly Dictionary<SwgId, AppearanceRequest> requests = new Dictionary<SwgId, AppearanceRequest>();
        readonly Dictionary<SwgId, string> loadedKeys = new Dictionary<SwgId, string>();
        readonly Dictionary<SwgId, AppearanceData> appearances = new Dictionary<SwgId, AppearanceData>();
        readonly HashSet<SwgId> dirtyAppearances = new HashSet<SwgId>();
        readonly HashSet<SwgId> pendingAppearances = new HashSet<SwgId>();
        readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        WorldState state;
        WorldContentLoader content;
        GameReplication source;
        SceneObjectRegistry registry;
        WorldAppearanceLibrary appearanceLibrary;
        WorldFloraRenderer flora;
        int uploadsPerFrame;
        bool loading;
        public IEnumerable<string> AppearanceKeys => appearances.Values.Select(appearance => appearance.Key);

        public void Initialize(WorldState state, WorldContentLoader content, WorldLoadingData loading, GameReplication world, WorldAppearanceLibrary appearanceLibrary, int uploadBudget, WorldFloraRenderer flora)
        {
            this.state = state;
            this.content = content;
            source = world;
            registry = world.SceneObjects;
            this.appearanceLibrary = appearanceLibrary;
            this.flora = flora;
            uploadsPerFrame = uploadBudget;
            registry.Removed += OnObjectRemoved;
            registry.ObjectChanged += OnObjectChanged;
            // A single initial pass; subsequent updates identify their affected object IDs.
            foreach (var sceneObject in source.CaptureSnapshot().Objects)
                RefreshAppearance(sceneObject.SwgId);
            foreach (var pair in loading.InitialAppearances)
                if (registry.TryGet<SpatialSceneObject>(pair.Key, out var sceneObject) && requests.TryGetValue(pair.Key, out var request) && pair.Value.Key == request.Key)
                {
                    Show(sceneObject, pair.Value);
                    loadedKeys[pair.Key] = request.Key;
                    pendingAppearances.Remove(pair.Key);
                }

            loading.InitialAppearances.Clear();
        }

        public void ShowScenery(SwgId swgId, AppearanceData appearance)
        {
            if (registry.TryGet<SpatialSceneObject>(swgId, out var sceneObject))
                Show(sceneObject, appearance);
        }

        public void RemoveScenery(SwgId swgId) => RemoveInstance(swgId);
        void OnObjectChanged(SceneObject sceneObject, SceneObjectChange changes)
        {
            if ((changes & (SceneObjectChange.Containment | SceneObjectChange.Replaced)) != 0 && sceneObject is CreatureSceneObject rider && rider.Mount != null && ManagedSceneObject.TryGetObjectFrom(rider.SwgId, out var instance) && instance.GetComponent<VehicleRider>() == null && instance.TryGetComponent<CharacterAnimation>(out _))
                instance.AddComponent<VehicleRider>().Initialize(rider);
            if ((changes & (SceneObjectChange.Appearance | SceneObjectChange.Containment)) != 0)
                dirtyAppearances.Add(sceneObject.SwgId);
        }

        void LateUpdate()
        {
            // Coalesce multiple baseline/delta packets for the same object within this frame.
            foreach (var swgId in dirtyAppearances)
                RefreshAppearance(swgId);
            dirtyAppearances.Clear();
            if (!loading && pendingAppearances.Count > 0)
                StartCoroutine(LoadPendingAppearances());
        }

        void RefreshAppearance(SwgId swgId)
        {
            registry.TryGet(swgId, out var sceneObject);
            var request = sceneObject == null ? null : AppearanceRequest.Describe(this.state, sceneObject);
            if (request == null)
            {
                // Scenery lifetime is managed by the streamer, not replicated appearance changes.
                if (requests.Remove(swgId))
                    RemoveInstance(swgId);
                loadedKeys.Remove(swgId);
                pendingAppearances.Remove(swgId);
                return;
            }

            requests[swgId] = request;
            if (ManagedSceneObject.TryGetObjectFrom(swgId, out var instance))
                instance.name = string.IsNullOrEmpty(sceneObject.Name) ? request.TemplatePath : sceneObject.Name;
            if (!loadedKeys.TryGetValue(swgId, out var key) || key != request.Key)
                pendingAppearances.Add(swgId);
            else
                pendingAppearances.Remove(swgId);
        }

        IEnumerator LoadPendingAppearances()
        {
            loading = true;
            try
            {
                var requested = pendingAppearances.ToDictionary(swgId => swgId, swgId => requests[swgId]);
                pendingAppearances.Clear();
                // Detached snapshots are needed only for asset work/cache writes, never movement.
                var task = content.LoadAppearancesAsync(source.CaptureSnapshot(), requested, lifetime.Token);
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
                    if (!registry.TryGet<SpatialSceneObject>(request.Key, out var sceneObject) || !requests.TryGetValue(request.Key, out var current) || current.Key != request.Value.Key)
                        continue;
                    loadedKeys[request.Key] = request.Value.Key;
                    pendingAppearances.Remove(request.Key);
                    if (task.Result.TryGetValue(request.Key, out var appearance))
                    {
                        try
                        {
                            Show(sceneObject, appearance);
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

        void Show(SpatialSceneObject sceneObject, AppearanceData appearance)
        {
            var instance = appearanceLibrary.Create(appearance, transform);
            instance.name = string.IsNullOrEmpty(sceneObject.Name) ? sceneObject.TemplatePath : sceneObject.Name;
            var localPose = SceneObjectMovement.GetLocalPose(state, sceneObject);
            instance.transform.localPosition = localPose.position;
            instance.transform.localRotation = localPose.rotation;
            instance.transform.localScale = Vector3.one * sceneObject.Scale;
            // Preserve the displayed pose if an appearance finishes loading during interpolation.
            if (ManagedSceneObject.TryGetObjectFrom(sceneObject.SwgId, out var previous))
            {
                instance.transform.localPosition = previous.transform.localPosition;
                instance.transform.localRotation = previous.transform.localRotation;
            }

            bool wasMovingPlayer = previous != null && previous.TryGetComponent<PlayerMovement>(out var previousMovement) && previousMovement.enabled;
            RemoveInstance(sceneObject.SwgId);
            var managed = instance.AddComponent<ManagedSceneObject>();
            managed.Bind(sceneObject.SwgId);
            if (this.state.Templates.IsTargetable(sceneObject.TemplatePath))
                instance.AddComponent<TargetableSceneObject>().Initialize(sceneObject.SwgId, appearance, sceneObject is CreatureSceneObject);
            instance.AddComponent<SceneObjectMovement>().Initialize(sceneObject);
            if (sceneObject is CreatureSceneObject vehicle && instance.TryGetComponent<VehicleExhaust>(out var exhaust))
                exhaust.Bind(vehicle, state.Templates.CreatureMovement(sceneObject.TemplatePath).RunSpeed);
            if (sceneObject is CreatureSceneObject creature && instance.TryGetComponent<CharacterAnimation>(out var animation))
            {
                instance.AddComponent<SceneObjectAnimation>().Initialize(creature, animation);
                if (creature.Mount != null) instance.AddComponent<VehicleRider>().Initialize(creature);
            }
            if (sceneObject.SwgId == this.state.PlayerSwgId)
            {
                var movement = instance.AddComponent<PlayerMovement>();
                movement.Initialize(sceneObject);
                movement.enabled = wasMovingPlayer;
            }

            var clearances = content.GetFloraClearance(this.state.Templates.Resolve(sceneObject.TemplateCrc) ?? sceneObject.TemplatePath);
            if (clearances.Length > 0)
                instance.AddComponent<FloraExclusion>().Initialize(this.state, flora, sceneObject.SwgId, clearances);
            appearances[sceneObject.SwgId] = appearance;
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

            appearances.Remove(swgId);
        }

        void OnObjectRemoved(SceneObject sceneObject)
        {
            RemoveInstance(sceneObject.SwgId);
            requests.Remove(sceneObject.SwgId);
            loadedKeys.Remove(sceneObject.SwgId);
            dirtyAppearances.Remove(sceneObject.SwgId);
            pendingAppearances.Remove(sceneObject.SwgId);
        }

        void OnDestroy()
        {
            lifetime.Cancel();
            lifetime.Dispose();
            if (registry == null)
                return;
            registry.Removed -= OnObjectRemoved;
            registry.ObjectChanged -= OnObjectChanged;
            foreach (var swgId in appearances.Keys.ToArray())
                RemoveInstance(swgId);
        }
    }
}
