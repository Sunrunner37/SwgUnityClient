using Swg.Client.Core.Characters;
using Swg.Client.Runtime.Commands;
using Swg.Client.Core.World;
using Swg.Client.Core;
using Swg.Client.Runtime.Assets.Appearances;
using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;
using UnityEngine;
using Swg.Client.Runtime.Networking;
using Swg.Client.Runtime.Assets.Tre;

namespace Swg.Client.Runtime.World.SceneObjects
{
    /// <summary>Binds replicated creature posture to its native skeletal animation without rebuilding the appearance.</summary>
    [DisallowMultipleComponent]
    public sealed class SceneObjectAnimation : MonoBehaviour
    {
        SceneObjectRegistry registry;
        CreatureSceneObject creature;
        SwgId swgId;
        CharacterAnimation animation;
        ClientCombat combat;
        TreRuntimeAssets assets;
        ClientCombatQueue queue;
        uint? appliedRecovery;
        bool awaitingDuration;
        void Play(string name, string weapon, bool reaction)
        {
            double? endsAt = null;
            uint? counter = null;
            if (!reaction && queue != null && queue.TryGetRecovery(Time.realtimeSinceStartupAsDouble, out var recovery) && recovery.ActionCounter != appliedRecovery)
            {
                endsAt = recovery.EndsAt;
                counter = recovery.ActionCounter;
            }
            if (!animation.PlayAction(assets, name, weapon, reaction, endsAt, creature.PerformanceType > 0 ? creature.PerformanceAnimation : "")) return;
            awaitingDuration = !reaction && queue != null && !endsAt.HasValue;
            if (counter.HasValue) appliedRecovery = counter;
        }
        void OnRecoveryStarted(uint counter, double endsAt)
        {
            if (!awaitingDuration) return;
            animation.SetActionDeadline(endsAt);
            appliedRecovery = counter;
            awaitingDuration = false;
        }
        void RefreshPerformance() => animation.SetAmbient(assets, creature.PerformanceType > 0 ? creature.PerformanceAnimation : "", creature.MoodAnimation);
        void RefreshStance() => animation.SetCombatStance(assets, combat.WeaponStance(creature), creature.IsInCombat);
        public void Initialize(CreatureSceneObject creature, CharacterAnimation animation)
        {
            this.creature = creature;
            swgId = creature.SwgId;
            this.animation = animation;
            registry = GameServices.Resolve<WorldState>().SceneObjects;
            combat = GameServices.Resolve<ClientCombat>();
            assets = GameServices.Resolve<TreRuntimeAssets>();
            if (swgId == GameServices.Resolve<CurrentPlayer>().SwgId)
            {
                queue = GameServices.Resolve<ClientCombatQueue>();
                queue.RecoveryStarted += OnRecoveryStarted;
            }
            combat.Subscribe(swgId, Play);
            registry.Subscribe(swgId, OnChanged);
            animation.SetPosture(creature.Posture, true);
            RefreshStance();
            RefreshPerformance();
        }
        void OnChanged(SceneObjectChange changes)
        {
            if ((changes & SceneObjectChange.Replaced) != 0)
                registry.TryGet<CreatureSceneObject>(swgId, out creature);
            if (creature != null && (changes & (SceneObjectChange.Posture | SceneObjectChange.Replaced)) != 0)
                animation.SetPosture(creature.Posture);
            if (creature != null && (changes & (SceneObjectChange.Performance | SceneObjectChange.Replaced)) != 0)
                RefreshPerformance();
            if (creature != null && (changes & (SceneObjectChange.State | SceneObjectChange.Posture | SceneObjectChange.Replaced)) != 0)
                RefreshStance();
        }
        void OnDestroy()
        {
            registry?.Unsubscribe(swgId, OnChanged);
            combat?.Unsubscribe(swgId, Play);
            if (queue != null) queue.RecoveryStarted -= OnRecoveryStarted;
        }
    }
}
