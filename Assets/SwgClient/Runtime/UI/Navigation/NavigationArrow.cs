using UnityEngine;
using UnityEngine.UI;

namespace Swg.Client.Runtime.UI.Navigation
{
    /// <summary>A camera-relative ground bearing, drawn on a ring around the player.</summary>
    public sealed class NavigationArrow : MonoBehaviour
    {
        public RawImage image;
        public float radius = 90;
        [Tooltip("Rotation needed to make the source artwork point upward.")]
        public float artworkRotation;

        public void Show(Camera camera, Vector3 playerPosition, Vector3 destination)
        {
            var direction = Bearing(camera, playerPosition, destination);
            var screen = camera.WorldToScreenPoint(playerPosition + Vector3.up);
            if (screen.z <= camera.nearClipPlane || direction == Vector2.zero)
            {
                gameObject.SetActive(false);
                return;
            }
            var rect = (RectTransform)transform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)rect.parent, screen, null, out var center);
            rect.anchoredPosition = center + direction * radius;
            rect.localRotation = Quaternion.Euler(0, 0, Angle(direction) + artworkRotation);
            gameObject.SetActive(true);
        }

        internal static Vector2 Bearing(Camera camera, Vector3 from, Vector3 to)
        {
            var delta = Vector3.ProjectOnPlane(to - from, Vector3.up);
            var forward = Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up).normalized;
            var right = Vector3.Cross(Vector3.up, forward);
            return new Vector2(Vector3.Dot(delta, right), Vector3.Dot(delta, forward)).normalized;
        }

        internal static float Angle(Vector2 direction) => Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90;
    }
}
