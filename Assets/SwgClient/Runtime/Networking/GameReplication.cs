using Swg.Client.Core.Networking.Game;
using Swg.Client.Core.Networking.Game.Replication;
using UnityEngine;
using Swg.Client.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using Swg.Client.Core.World;
using Swg.Client.Core.World.SceneObjects;
using Swg.Client.Core.Networking.Game.Packets.Scene;

namespace Swg.Client.Runtime.Networking
{
    /// <summary>Queues decoded network changes; Unity Update applies them to the registry on the main thread.</summary>
    public sealed class GameReplication : MonoBehaviour, IDisposable
    {
        sealed class ReplicationState
        {
            public bool BaselinesComplete;
            public uint MovementCounter;
        }

        readonly ConcurrentQueue<Action> pending = new ConcurrentQueue<Action>();
        readonly Dictionary<SwgId, ReplicationState> replication = new Dictionary<SwgId, ReplicationState>();
        CmdStartScenePacket? startScene;
        bool sceneReadyAcknowledged;
        volatile bool disposed;
        long revision;
        public WorldState State { get; private set; }
        public SceneObjectRegistry SceneObjects => State.SceneObjects;

        public void Initialize(WorldState state) => State = state;
        void Update() => ApplyPending();
        void OnDestroy() => StopReceiving();
        public long Revision => revision;

        public void ApplyPending()
        {
            while (pending.TryDequeue(out var apply))
                apply();
        }

        public GameSceneStatus CaptureSceneStatus()
        {
            bool complete = startScene.HasValue && replication.TryGetValue(startScene.Value.PlayerSwgId, out var player) && player.BaselinesComplete;
            return new GameSceneStatus(startScene, complete, sceneReadyAcknowledged);
        }

        public GameWorldSnapshot CaptureSnapshot()
        {
            var status = CaptureSceneStatus();
            var objects = new List<SceneObject>(replication.Count);
            foreach (var swgId in replication.Keys)
                if (SceneObjects.TryGet(swgId, out var sceneObject))
                    objects.Add(sceneObject.Copy());
            return new GameWorldSnapshot(startScene, status.PlayerBaselinesComplete, sceneReadyAcknowledged, revision, objects.ToArray());
        }

        internal void StartScene(CmdStartScenePacket start)
        {
            if (start.PlayerSwgId == 0)
                throw new InvalidDataException("Zero replicated object ID");
            if (disposed)
                return;
            pending.Enqueue(() =>
            {
                State.BeginScene(start.PlayerSwgId, start.Position, start.TerrainPath);
                replication.Clear();
                startScene = start;
                sceneReadyAcknowledged = false;
                var player = ReplicatedSceneObjectFactory.Create(start);
                SceneObjects.Add(player);
                revision++;
                replication.Add(start.PlayerSwgId, new ReplicationState());
            });
        }

        internal void UpdateCommands(Action<List<string>> update)
        {
            if (!disposed)
                pending.Enqueue(() => State.Commands.Apply(update));
        }

        internal void AcknowledgeSceneReady()
        {
            if (disposed)
                return;
            pending.Enqueue(() =>
            {
                sceneReadyAcknowledged = true;
                revision++;
            });
        }

        internal void Remove(SwgId swgId)
        {
            if (disposed)
                return;
            pending.Enqueue(() =>
            {
                SceneObjects.Remove(swgId);
                replication.Remove(swgId);
                revision++;
            });
        }

        internal void Commit(SwgId swgId, GameObjectUpdate update)
        {
            if (swgId == 0)
                throw new InvalidDataException("Zero replicated object ID");
            if (disposed)
                return;
            var owned = update.CopyForQueue();
            pending.Enqueue(() =>
            {
                if (!SceneObjects.TryGet(swgId, out var sceneObject))
                {
                    sceneObject = ReplicatedSceneObjectFactory.Create(swgId, owned, State.Templates);
                    SceneObjects.Add(sceneObject);
                }
                else
                {
                    var updated = ReplicatedSceneObjectFactory.Apply(sceneObject, owned, State.Templates);
                    if (!ReferenceEquals(sceneObject, updated))
                    {
                        SceneObjects.Replace(updated);
                        sceneObject = updated;
                    }
                }

                if (owned.ParentSwgId.HasValue)
                    SceneObjects.Reparent(swgId, owned.ParentSwgId.Value);
                SceneObjects.NotifyChanged(sceneObject, owned.Changes);
                revision++;
                if (!replication.TryGetValue(swgId, out var state))
                    replication.Add(swgId, state = new ReplicationState());
                if (owned.BaselinesComplete.HasValue)
                    state.BaselinesComplete = owned.BaselinesComplete.Value;
                if (owned.MovementCounter.HasValue)
                    state.MovementCounter = owned.MovementCounter.Value;
            });
        }

        // Main-thread local prediction shares the registry and targeted pose notifications with replication.
        public uint NextPlayerMovementCounter()
        {
            return ++replication[State.PlayerSwgId].MovementCounter;
        }

        public void SetTemplates(ObjectTemplateCatalog catalog)
        {
            State.Templates = catalog;
            foreach (var swgId in replication.Keys)
            {
                if (!SceneObjects.TryGet(swgId, out var current))
                    continue;
                var updated = ReplicatedSceneObjectFactory.ResolveTemplate(current, catalog);
                if (!ReferenceEquals(current, updated))
                    SceneObjects.Replace(updated);
                else
                    SceneObjects.NotifyChanged(current, SceneObjectChange.Appearance);
            }
        }

        public void Dispose()
        {
            if (disposed)
                return;
            StopReceiving();
            enabled = false;
            if (Application.isPlaying)
                Destroy(gameObject);
            else
                DestroyImmediate(gameObject);
        }

        void StopReceiving()
        {
            disposed = true;
            while (pending.TryDequeue(out _))
            {
            }

            replication.Clear();
        }
    }
}
