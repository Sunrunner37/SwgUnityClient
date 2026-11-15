using Swg.Client.Core.Characters;
using Swg.Client.Core.World.SceneObjects;
using Swg.Client.Runtime.UI.Navigation;
using System;
using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;
using Swg.Client.Core;
using Swg.Client.Core.World;
using Swg.Client.Runtime.Localization;
using Swg.Client.Runtime.World.SceneObjects;
using TMPro;
using UnityEngine;
using Swg.Client.Runtime.UI.Hud;

namespace Swg.Client.Runtime.UI.Targeting
{
    /// <summary>Displays the selected object's name and creature HAM, plus projected appearance corners.</summary>
    [DefaultExecutionOrder(200)]
    public sealed class TargetHud : MonoBehaviour
    {
        [Serializable]
        public struct PostureVisual
        {
            public CreaturePosture posture;
            public GameObject icon;
        }

        public GameObject panel;
        public TMP_Text targetName;
        public RectTransform selection;
        public RectTransform hover;
        public NavigationArrow directionArrow;
        public HamBars ham;
        public GameObject posture;
        public PostureVisual[] postureIcons;
        public Camera worldCamera;
        ClientTargeting targeting;
        WorldState state;
        CurrentPlayer currentPlayer;
        SceneObjectNameResolver names;
        SwgId displayedSwgId;
        string displayedName;
        string displayedTemplate;
        string localizedName;
        int displayedDistance = int.MinValue;

        void Awake()
        {
            targeting = GameServices.Resolve<ClientTargeting>();
            currentPlayer = GameServices.Resolve<CurrentPlayer>();
            state = GameServices.Resolve<WorldState>();
            names = GameServices.Resolve<SceneObjectNameResolver>();
        }

        void OnEnable()
        {
            if (worldCamera == null)
                worldCamera = Camera.main;
        }

        void LateUpdate()
        {
            bool selected = state.SceneObjects.TryGet(targeting.SelectedSwgId, out var sceneObject);
            panel.SetActive(selected);
            UpdateIndicator(selection, targeting.SelectedSwgId);
            UpdateIndicator(hover, targeting.HoveredSwgId == targeting.SelectedSwgId ? default : targeting.HoveredSwgId);
            UpdateDirectionArrow();
            if (!selected)
                return;
            int distance = -1;
            if (ManagedSceneObject.TryGetObjectFrom(currentPlayer.SwgId, out var player) && ManagedSceneObject.TryGetObjectFrom(sceneObject.SwgId, out var target))
                distance = Mathf.RoundToInt(Vector3.Distance(player.transform.position, target.transform.position));
            if (displayedSwgId != sceneObject.SwgId || displayedName != sceneObject.Name || displayedTemplate != sceneObject.TemplatePath)
            {
                displayedSwgId = sceneObject.SwgId;
                displayedName = sceneObject.Name;
                displayedTemplate = sceneObject.TemplatePath;
                localizedName = names.Resolve(sceneObject);
                displayedDistance = int.MinValue;
            }
            if (displayedDistance != distance)
            {
                displayedDistance = distance;
                targetName.text = localizedName + (distance >= 0 ? " (" + distance + "m)" : "");
            }
            var creature = sceneObject as CreatureSceneObject;
            bool hasHam = creature != null && creature.Ham.HasAnAttackableElement;
            ham.gameObject.SetActive(hasHam);
            UpdatePosture(creature);
            if (!hasHam)
                return;
            ham.Show(creature.Ham);
        }

        void UpdatePosture(CreatureSceneObject creature)
        {
            bool applicable = false;
            foreach (var visual in postureIcons)
            {
                bool active = creature != null && creature.Posture == visual.posture;
                visual.icon.SetActive(active);
                applicable |= active;
            }
            posture.SetActive(applicable);
        }

        void UpdateDirectionArrow()
        {
            // Any visible portion of the selection box suppresses the direction arrow.
            if (selection.gameObject.activeSelf || worldCamera == null
                || !state.SceneObjects.TryGet<SpatialSceneObject>(targeting.SelectedSwgId, out _)
                || !ManagedSceneObject.TryGetObjectFrom(currentPlayer.SwgId, out var player))
            {
                directionArrow.gameObject.SetActive(false);
                return;
            }
            var destination = state.SceneObjects.GetServerWorldPose(targeting.SelectedSwgId).position - state.RenderOrigin;
            if (ManagedSceneObject.TryGetObjectFrom(targeting.SelectedSwgId, out var target))
                destination = target.transform.position;
            directionArrow.Show(worldCamera, player.transform.position, destination);
        }

        void UpdateIndicator(RectTransform indicator, SwgId swgId)
        {
            if (worldCamera == null || !ManagedSceneObject.TryGetObjectFrom(swgId, out var instance) || !instance.TryGetComponent<TargetableSceneObject>(out var target))
            {
                indicator.gameObject.SetActive(false);
                return;
            }
            if (!TryProjectSelection(worldCamera, target, out var projected))
            {
                indicator.gameObject.SetActive(false);
                return;
            }
            var parent = (RectTransform)indicator.parent;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, projected.min, null, out var lower);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, projected.max, null, out var upper);
            indicator.anchoredPosition = (lower + upper) * 0.5f;
            indicator.sizeDelta = Vector2.Max(upper - lower, Vector2.one * 40);
            indicator.gameObject.SetActive(true);
        }

        static bool TryProjectSelection(Camera camera, TargetableSceneObject target, out Rect rectangle)
        {
            return target.SelectionGeometry.Length == 0
                ? TryProjectBounds(camera, target.transform.localToWorldMatrix, target.SelectionBounds, out rectangle)
                : TryProjectGeometry(camera, target.transform.localToWorldMatrix, target.SelectionBounds, target.SelectionGeometry, out rectangle);
        }

        // Actual vertices give sparse structures (masts, signs, harvesters) a tight screen rectangle.
        internal static bool TryProjectGeometry(Camera camera, Matrix4x4 localToWorld, Bounds bounds, Vector3[][] parts, out Rect rectangle)
        {
            var toCamera = camera.worldToCameraMatrix * localToWorld;
            var projection = camera.projectionMatrix;
            var viewport = camera.pixelRect;
            var minimum = new Vector2(float.MaxValue, float.MaxValue);
            var maximum = new Vector2(float.MinValue, float.MinValue);
            foreach (var vertices in parts)
                foreach (var vertex in vertices)
                {
                    var point = toCamera.MultiplyPoint3x4(vertex);
                    // Keep conservative clipping for geometry intersecting the camera's near plane.
                    if (-point.z < camera.nearClipPlane)
                        return TryProjectBounds(camera, localToWorld, bounds, out rectangle);
                    var clip = projection * new Vector4(point.x, point.y, point.z, 1);
                    var screen = new Vector2(viewport.x + (clip.x / clip.w + 1) * viewport.width * 0.5f,
                        viewport.y + (clip.y / clip.w + 1) * viewport.height * 0.5f);
                    minimum = Vector2.Min(minimum, screen);
                    maximum = Vector2.Max(maximum, screen);
                }
            return ClipToViewport(camera, minimum, maximum, out rectangle);
        }

        // Clip the box's edges at the near plane so a partly visible structure retains its indicators.
        internal static bool TryProjectBounds(Camera camera, Matrix4x4 localToWorld, Bounds bounds, out Rect rectangle)
        {
            var toCamera = camera.transform.worldToLocalMatrix * localToWorld;
            var toWorld = camera.transform.localToWorldMatrix;
            float near = camera.nearClipPlane;
            Span<Vector3> corners = stackalloc Vector3[8];
            var minimum = new Vector2(float.MaxValue, float.MaxValue);
            var maximum = new Vector2(float.MinValue, float.MinValue);

            void Include(Vector3 point)
            {
                var projected = camera.WorldToScreenPoint(toWorld.MultiplyPoint3x4(point));
                minimum = Vector2.Min(minimum, projected);
                maximum = Vector2.Max(maximum, projected);
            }

            for (int i = 0; i < corners.Length; i++)
            {
                corners[i] = toCamera.MultiplyPoint3x4(TargetableSceneObject.Corner(bounds, i));
                if (corners[i].z >= near)
                    Include(corners[i]);
            }
            // Toggling one bit connects adjacent corners: twelve unique edges in total.
            for (int i = 0; i < corners.Length; i++)
                for (int bit = 1; bit <= 4; bit <<= 1)
                {
                    int other = i ^ bit;
                    if (other < i)
                        continue;
                    var a = corners[i];
                    var b = corners[other];
                    if ((a.z >= near) == (b.z >= near))
                        continue;
                    Include(Vector3.LerpUnclamped(a, b, (near - a.z) / (b.z - a.z)));
                }
            return ClipToViewport(camera, minimum, maximum, out rectangle);
        }

        static bool ClipToViewport(Camera camera, Vector2 minimum, Vector2 maximum, out Rect rectangle)
        {
            if (minimum.x == float.MaxValue)
            {
                rectangle = default;
                return false;
            }
            minimum = Vector2.Max(minimum, camera.pixelRect.min);
            maximum = Vector2.Min(maximum, camera.pixelRect.max);
            rectangle = Rect.MinMaxRect(minimum.x, minimum.y, maximum.x, maximum.y);
            return maximum.x > minimum.x && maximum.y > minimum.y;
        }
    }
}
