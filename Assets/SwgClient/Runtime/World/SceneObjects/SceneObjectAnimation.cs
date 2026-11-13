using Swg.Client.Core.World;
using Swg.Client.Core;
using Swg.Client.Runtime.Assets.Appearances;
using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;
using UnityEngine;

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
        public void Initialize(CreatureSceneObject creature, CharacterAnimation animation)
        {
            this.creature = creature;
            swgId = creature.SwgId;
            this.animation = animation;
            registry = GameServices.Resolve<WorldState>().SceneObjects;
            registry.Subscribe(swgId, OnChanged);
            animation.SetPosture(creature.Posture, true);
        }
        void OnChanged(SceneObjectChange changes)
        {
            if ((changes & SceneObjectChange.Replaced) != 0)
                registry.TryGet<CreatureSceneObject>(swgId, out creature);
            if (creature != null && (changes & (SceneObjectChange.Posture | SceneObjectChange.Replaced)) != 0)
                animation.SetPosture(creature.Posture);
        }
        void OnDestroy()
        {
            registry?.Unsubscribe(swgId, OnChanged);
        }
    }
}
