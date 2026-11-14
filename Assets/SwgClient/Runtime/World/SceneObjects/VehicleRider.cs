using Swg.Client.Core;
using Swg.Client.Core.World;
using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;
using Swg.Client.Runtime.Assets.Appearances;
using UnityEngine;

namespace Swg.Client.Runtime.World.SceneObjects
{
    /// <summary>Follows the mount's rendered seat without making the rider owned by the vehicle's GameObject lifetime.</summary>
    [DefaultExecutionOrder(-50)]
    [DisallowMultipleComponent]
    public sealed class VehicleRider : MonoBehaviour
    {
        SceneObjectRegistry registry;
        CreatureSceneObject creature;
        CharacterAnimation animation;
        SwgId swgId;
        SwgId mountSwgId;
        VehicleSeat seat;

        public void Initialize(CreatureSceneObject creature)
        {
            this.creature = creature;
            swgId = creature.SwgId;
            animation = GetComponent<CharacterAnimation>();
            registry = GameServices.Resolve<WorldState>().SceneObjects;
            registry.Subscribe(swgId, OnChanged);
            OnChanged(SceneObjectChange.Containment);
        }
        void OnChanged(SceneObjectChange changes)
        {
            if ((changes & SceneObjectChange.Replaced) != 0)
                registry.TryGet<CreatureSceneObject>(swgId, out creature);
            if (creature == null || creature.Mount == null)
            {
                animation.SetRidingPose(null);
                if (creature != null) transform.localScale = Vector3.one * creature.Scale;
                enabled = false;
                Destroy(this);
                return;
            }
            if (mountSwgId != creature.Mount.SwgId)
            {
                mountSwgId = creature.Mount.SwgId;
                seat = null;
            }
        }
        void LateUpdate()
        {
            if (seat == null && ManagedSceneObject.TryGetObjectFrom(mountSwgId, out var mount))
                seat = mount.GetComponentInChildren<VehicleSeat>();
            if (seat == null) return; // The mount's appearance may still be loading.
            animation.SetRidingPose(seat.riderPose);
            transform.localScale = Vector3.one; // Native riders temporarily use unit appearance scale.
            transform.SetPositionAndRotation(seat.transform.position, seat.transform.rotation);
        }
        void OnDestroy()
        {
            registry?.Unsubscribe(swgId, OnChanged);
        }
    }
}
