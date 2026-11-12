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
            worldCamera.transform.SetPositionAndRotation(focus - rotation * Vector3.forward * distance, rotation);
        }
    }
}
