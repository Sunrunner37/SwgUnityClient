using Swg.Client.Core.World;
using Swg.Client.Core.World.SceneObjects;
using Swg.Client.Core.Characters;
using Swg.Client.Runtime.World.SceneObjects;
using UnityEngine;
using Swg.Client.Runtime.UI;
using UnityEngine.InputSystem;

namespace Swg.Client.Runtime.World
{
    public sealed class WorldOrbitCamera : MonoBehaviour
    {
        public Camera worldCamera;
        public Transform target;
        public float distance = 7;
        public float pitch = 18;
        public float yaw = 180;
        public float sensitivity = .18f;
        readonly RaycastHit[] obstacleHits = new RaycastHit[64];
        WorldState state;
        CurrentPlayer player;
        void Start()
        {
            state = GameServices.Resolve<WorldState>();
            player = GameServices.Resolve<CurrentPlayer>();
        }
        internal Vector3 ConstrainToObstacles(Vector3 focus, Vector3 desired, Transform mount = null)
        {
            var offset = desired - focus;
            float nearest = float.PositiveInfinity;
            int mask = ~(1 << LayerMask.NameToLayer(TargetableSceneObject.LayerName));
            int count = Physics.SphereCastNonAlloc(focus, .18f, offset.normalized, obstacleHits, offset.magnitude, mask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var hit = obstacleHits[i];
                // The movement capsule is solid and intentionally isn't on the target-picking layer.
                // A moving character can briefly sweep against its own capsule near the camera focus.
                var body = hit.collider.transform;
                if (body.IsChildOf(target) || mount != null && body.IsChildOf(mount))
                    continue;
                nearest = Mathf.Min(nearest, hit.distance);
            }
            return focus + offset.normalized * Mathf.Min(offset.magnitude, Mathf.Max(.3f, nearest - .08f));
        }

        void LateUpdate()
        {
            if (target == null || worldCamera == null)
                return;
            var mouse = Mouse.current;
            if (!UiInput.PointerOverUi)
            {
                if (mouse.rightButton.isPressed)
                {
                    var delta = mouse.delta.ReadValue();
                    yaw += delta.x * sensitivity;
                    pitch = Mathf.Clamp(pitch - delta.y * sensitivity, -10, 80);
                }

                distance = Mathf.Clamp(distance - mouse.scroll.ReadValue().y * .012f, 2, 60);
            }

            var rotation = Quaternion.Euler(pitch, yaw, 0);
            var focus = target.position + Vector3.up * 1.4f;
            var desired = focus - rotation * Vector3.forward * distance;
            Transform mount = null;
            if (player.Creature?.Mount != null && ManagedSceneObject.TryGetObjectFrom(player.Creature.Mount.SwgId, out var vehicle))
                mount = vehicle.transform;
            desired = ConstrainToObstacles(focus, desired, mount);
            if (state.Terrain != null && player.Creature?.Parent is not CellSceneObject)
            {
                var serverPosition = desired + state.RenderOrigin;
                desired.y = Mathf.Max(desired.y, state.Terrain.SampleHeight(serverPosition.x, serverPosition.z) - state.RenderOrigin.y + .25f);
            }
            worldCamera.transform.SetPositionAndRotation(desired, rotation);
        }
    }
}
