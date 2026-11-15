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
        WorldState state;
        CurrentPlayer player;
        void Start()
        {
            state = GameServices.Resolve<WorldState>();
            player = GameServices.Resolve<CurrentPlayer>();
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
            int mask = ~(1 << LayerMask.NameToLayer(TargetableSceneObject.LayerName));
            if (Physics.SphereCast(focus, .18f, (desired - focus).normalized, out var hit, distance, mask, QueryTriggerInteraction.Ignore))
                desired = focus + (desired - focus).normalized * Mathf.Max(.3f, hit.distance - .08f);
            if (state.Terrain != null && player.Creature?.Parent is not CellSceneObject)
            {
                var serverPosition = desired + state.RenderOrigin;
                desired.y = Mathf.Max(desired.y, state.Terrain.SampleHeight(serverPosition.x, serverPosition.z) - state.RenderOrigin.y + .25f);
            }
            worldCamera.transform.SetPositionAndRotation(desired, rotation);
        }
    }
}
