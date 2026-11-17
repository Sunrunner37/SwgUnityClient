using Swg.Client.Core.Characters;
using Swg.Client.Core.Crafting;
using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;
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
using Swg.Client.Core.Networking.Game.Packets.Combat;
using Swg.Client.Core.Networking.Game.Packets.Movement;

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

        CurrentPlayer currentPlayer;
        CraftingProgress craftingProgress;
        Action<TeleportAckPacket> sendTeleportAcknowledgement;
        public void Initialize(WorldState state, CurrentPlayer currentPlayer, CraftingProgress craftingProgress, Action<TeleportAckPacket> sendTeleportAcknowledgement)
        {
            State = state;
            this.currentPlayer = currentPlayer;
            this.craftingProgress = craftingProgress;
            this.sendTeleportAcknowledgement = sendTeleportAcknowledgement;
        }
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
                currentPlayer.BeginScene(start.PlayerSwgId);
                State.BeginScene(start.Position, start.TerrainPath);
                replication.Clear();
                startScene = start;
                sceneReadyAcknowledged = false;
                var player = ReplicatedSceneObjectFactory.Create(start);
                SceneObjects.Add(player);
                revision++;
                replication.Add(start.PlayerSwgId, new ReplicationState());
            });
        }

        internal void UpdateCombatPostures(CombatActionPacket packet)
        {
            if (disposed) return;
            pending.Enqueue(() =>
            {
                // Non-creature defenders (buildings, terminals, etc.) also occur in combat packets.
                void ApplyPosture(SwgId swgId, byte posture)
                {
                    if (!SceneObjects.TryGet<CreatureSceneObject>(swgId, out var creature) || (byte)creature.Posture == posture) return;
                    creature.Apply(new CreatureSceneObjectUpdate { Posture = posture });
                    SceneObjects.NotifyChanged(creature, SceneObjectChange.Posture);
                    revision++;
                }
                ApplyPosture(packet.AttackerSwgId, packet.AttackerPosture);
                foreach (var defender in packet.Defenders) ApplyPosture(defender.SwgId, defender.Posture);
            });
        }

        internal void UpdateWaypoints(Action<Dictionary<SwgId, Waypoint>> update)
        {
            if (!disposed)
                pending.Enqueue(() => currentPlayer.UpdateWaypoints(update));
        }

        internal void UpdatePlayer(Action<CurrentPlayer> update, Action<CraftingProgress> updateCrafting = null)
        {
            if (!disposed)
                pending.Enqueue(() => { update(currentPlayer); updateCrafting?.Invoke(craftingProgress); });
        }

        internal void UpdateDraftDetails(uint crc, DraftSchematicDetails details)
        {
            if (!disposed)
                pending.Enqueue(() => currentPlayer.UpdateDraftDetails(crc, details));
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

        void Enqueue(SwgId swgId, Action apply)
        {
            if (swgId == 0)
                throw new InvalidDataException("Zero replicated object ID");
            if (!disposed)
                pending.Enqueue(apply);
        }

        internal void Commit(SwgId swgId, SceneObjectUpdate update)
        {
            var owned = update.CopyForQueue();
            Enqueue(swgId, () => ApplyObject(swgId, owned.RequiredType, owned.ApplyTo, owned.Changes));
        }

        internal void Commit(SwgId swgId, MovementUpdate update, bool isBroadcast = false)
        {
            Enqueue(swgId, () =>
            {
                // Core3 echoes extrapolated vehicle poses to their rider; local input owns that prediction.
                if (isBroadcast && SceneObjects.TryGet<CreatureSceneObject>(currentPlayer.SwgId, out var localPlayer) && localPlayer.Mount?.SwgId == swgId && (localPlayer.States & CreatureState.RidingMount) != 0)
                    return;
                ApplyMovement(swgId, update, SceneObjectChange.Transform);
            });
        }

        internal void ApplyCorrection(SwgId swgId, MovementUpdate update)
        {
            Enqueue(swgId, () =>
            {
                var changes = SceneObjectChange.Transform;
                if (swgId == currentPlayer.SwgId) changes |= SceneObjectChange.Correction;
                var state = ApplyMovement(swgId, update, changes);
                // Core3 ignores further player movement until this arrives. Snap/reset prediction first.
                if (swgId == currentPlayer.SwgId)
                    sendTeleportAcknowledgement(new TeleportAckPacket(swgId, state.MovementCounter));
            });
        }

        ReplicationState ApplyMovement(SwgId swgId, MovementUpdate update, SceneObjectChange changes)
        {
            var parentSwgId = update.ParentSwgId;
            // World-space rider corrections retain mount containment and correct the controlled vehicle too.
            if (parentSwgId == 0 && SceneObjects.TryGet<CreatureSceneObject>(swgId, out var rider) && rider.Mount != null && (rider.States & CreatureState.RidingMount) != 0)
            {
                parentSwgId = null;
                rider.Mount.Apply(update);
                SceneObjects.NotifyChanged(rider.Mount, changes);
            }
            var state = ApplyObject(swgId, typeof(SpatialSceneObject), sceneObject => ((SpatialSceneObject)sceneObject).Apply(update),
                changes, parentSwgId: parentSwgId);
            if (update.MovementCounter.HasValue)
                state.MovementCounter = update.MovementCounter.Value;
            return state;
        }

        internal void Commit(SwgId swgId, ContainmentUpdate update)
        {
            Enqueue(swgId, () => ApplyObject(swgId, null, sceneObject => sceneObject.Apply(update),
                update.Arrangement.HasValue ? SceneObjectChange.Appearance : SceneObjectChange.None, parentSwgId: update.ParentSwgId));
        }

        // Baselines carry type metadata and object fields; creation carries template metadata and an initial pose.
        // Keep each packet atomic when it crosses to the main thread.
        internal void Commit(SwgId swgId, ReplicationLifecycleUpdate update, SceneObjectUpdate objectUpdate = null, MovementUpdate? initialMovement = null)
        {
            var owned = objectUpdate?.CopyForQueue();
            Enqueue(swgId, () =>
            {
                var requiredType = update.ObjectType.HasValue ? ReplicatedSceneObjectFactory.TypeFor(update.ObjectType.Value) : owned?.RequiredType;
                var changes = owned?.Changes ?? SceneObjectChange.None;
                if (update.TemplateCrc.HasValue || update.ObjectType.HasValue || update.BaselinesComplete)
                    changes |= SceneObjectChange.Appearance;
                if (initialMovement.HasValue)
                    changes |= SceneObjectChange.Transform;
                var state = ApplyObject(swgId, requiredType, sceneObject =>
                {
                    owned?.ApplyTo(sceneObject);
                    if (initialMovement.HasValue && sceneObject is SpatialSceneObject spatial)
                        spatial.Apply(initialMovement.Value);
                }, changes, update.TemplateCrc, initialMovement?.ParentSwgId);
                if (update.BaselinesComplete)
                    state.BaselinesComplete = true;
                if (initialMovement?.MovementCounter != null)
                    state.MovementCounter = initialMovement.Value.MovementCounter.Value;
            });
        }

        ReplicationState ApplyObject(SwgId swgId, Type requiredType, Action<SceneObject> apply, SceneObjectChange changes,
            uint? templateCrc = null, SwgId? parentSwgId = null)
        {
            bool exists = SceneObjects.TryGet(swgId, out var current);
            var sceneObject = exists
                ? ReplicatedSceneObjectFactory.Refine(current, State.Templates, requiredType, templateCrc)
                : ReplicatedSceneObjectFactory.Create(swgId, State.Templates, requiredType, templateCrc);
            if (!exists && parentSwgId.HasValue)
                sceneObject.ParentSwgId = parentSwgId.Value;
            apply(sceneObject);
            if (!exists)
                SceneObjects.Add(sceneObject);
            else if (!ReferenceEquals(current, sceneObject))
                SceneObjects.Replace(sceneObject);
            if (parentSwgId.HasValue)
                SceneObjects.Reparent(swgId, parentSwgId.Value);
            SceneObjects.NotifyChanged(sceneObject, changes);
            revision++;
            if (!replication.TryGetValue(swgId, out var state))
                replication.Add(swgId, state = new ReplicationState());
            return state;
        }

        // Main-thread local prediction shares the registry and targeted pose notifications with replication.
        public uint NextPlayerMovementCounter()
        {
            return ++replication[currentPlayer.SwgId].MovementCounter;
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
