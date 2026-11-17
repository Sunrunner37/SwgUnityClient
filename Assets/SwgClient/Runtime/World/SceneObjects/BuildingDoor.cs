using Swg.Client.Core.Assets.Appearances;
using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;
using Swg.Client.Core.Characters;
using Swg.Client.Core.World;
using Swg.Client.Core.World.SceneObjects;
using UnityEngine;

namespace Swg.Client.Runtime.World.SceneObjects
{
    /// <summary>Slides native door panels using the shared style's timing and proximity radius.</summary>
    public sealed class BuildingDoor : MonoBehaviour
    {
        DoorAppearance appearance;
        Transform first;
        Transform second;
        CurrentPlayer player;
        SceneObjectRegistry sceneObjects;
        readonly Collider[] nearby = new Collider[32];
        float progress;
        float nextCheck;
        bool open;

        public void Initialize(DoorAppearance appearance, Transform first, Transform second)
        {
            this.appearance = appearance;
            this.first = first;
            this.second = second;
        }

        void Start()
        {
            player = GameServices.Resolve<CurrentPlayer>();
            sceneObjects = GameServices.Resolve<SceneObjectRegistry>();
        }

        void Update()
        {
            if (Time.time >= nextCheck)
            {
                nextCheck = Time.time + .1f;
                var center = transform.TransformPoint(Vector3.up);
                float radius = appearance.TriggerRadius * transform.lossyScale.x;
                open = ManagedSceneObject.TryGetObjectFrom(player.SwgId, out var body) &&
                    (body.transform.position + Vector3.up - center).sqrMagnitude <= radius * radius;
                if (!open)
                {
                    int count = Physics.OverlapSphereNonAlloc(center, radius, nearby, 1 << LayerMask.NameToLayer(TargetableSceneObject.LayerName), QueryTriggerInteraction.Collide);
                    for (int i = 0; i < count && !open; i++)
                    {
                        var managed = nearby[i].GetComponentInParent<ManagedSceneObject>();
                        open = managed != null && sceneObjects.TryGet<CreatureSceneObject>(managed.SwgId, out _);
                    }
                }
            }
            Advance(Time.deltaTime, open);
        }

        internal void Advance(float seconds, bool shouldOpen)
        {
            float target = shouldOpen && appearance.Portal.Passable ? 1 : 0;
            float duration = target > progress ? appearance.OpenSeconds : appearance.CloseSeconds;
            progress = Mathf.MoveTowards(progress, target, duration > 0 ? seconds / duration : 1);
            // Same spring/cosine blend as native DoorObject::tween, applied to either sliding axis.
            float value = Mathf.Pow(progress, 1 / Mathf.Max(.001f, appearance.Spring));
            value = Mathf.LerpUnclamped(value, (1 - Mathf.Cos(value * Mathf.PI)) * .5f, appearance.Smoothness);
            if (first != null) first.localPosition = appearance.Movement * value;
            if (second != null) second.localPosition = -appearance.Movement * value;
        }
    }
}
