using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Swg.Client.Core.Characters;
using Swg.Client.Core.World;
using Swg.Client.Core.World.SceneObjects;
using Swg.Client.Runtime.World.SceneObjects;
using Swg.Client.Runtime.Localization;

namespace Swg.Client.Runtime.UI.Navigation.Minimap
{
    /// <summary>North-up local map with clamped waypoint bearings. Its 256px world capture runs at four Hz.</summary>
    public sealed class Minimap : MonoBehaviour, IScrollHandler
    {
        public RawImage map;
        public RectTransform bearings;
        public Image arrowPrefab;
        public RectTransform playerMarker;
        public TMP_Text rangeLabel;
        public TMP_Text planetLabel;
        public TMP_Text longitude;
        public TMP_Text elevation;
        public TMP_Text latitude;
        public float range = 32;
        public float refreshSeconds = .25f;
        readonly List<(Waypoint Waypoint, Image Arrow)> arrows = new List<(Waypoint, Image)>();
        CurrentPlayer player;
        WorldState state;
        Camera capture;
        RenderTexture texture;
        float nextCapture;
        void Awake()
        {
            player = GameServices.Resolve<CurrentPlayer>();
            state = GameServices.Resolve<WorldState>();
            var owner = new GameObject("Minimap capture");
            owner.transform.SetParent(transform, false);
            capture = owner.AddComponent<Camera>();
            capture.enabled = false;
            capture.orthographic = true;
            capture.clearFlags = CameraClearFlags.SolidColor;
            capture.backgroundColor = new Color32(89, 91, 82, 255);
            capture.cullingMask = 1; // World geometry only; excludes UI, target volumes and preview stages.
            capture.nearClipPlane = .1f;
            capture.farClipPlane = 600;
            capture.allowHDR = false;
            capture.allowMSAA = false;
            var data = owner.AddComponent<UniversalAdditionalCameraData>();
            data.renderShadows = false;
            data.renderPostProcessing = false;
            texture = new RenderTexture(256, 256, 16) { name = "Local minimap" };
            texture.Create();
            capture.targetTexture = texture;
            map.texture = texture;
        }
        void OnEnable()
        {
            player.WaypointsChanged += Rebuild;
            state.SceneStarted += Rebuild;
            Rebuild();
            nextCapture = 0;
        }
        void OnDisable()
        {
            player.WaypointsChanged -= Rebuild;
            state.SceneStarted -= Rebuild;
        }
        void Rebuild()
        {
            foreach (var item in arrows) Destroy(item.Arrow.gameObject);
            arrows.Clear();
            foreach (var waypoint in player.NavigationWaypoints)
            {
                if (!waypoint.Active || waypoint.PlanetCrc != state.PlanetCrc) continue;
                var arrow = Instantiate(arrowPrefab, bearings);
                arrow.gameObject.SetActive(true);
                arrow.color = WaypointHud.ColorFor(waypoint.Color);
                arrows.Add((waypoint, arrow));
            }
            string planet = System.IO.Path.GetFileNameWithoutExtension(state.TerrainPath);
            planetLabel.text = GameServices.Resolve<StringTableLocalizer>().Lookup("planet_n", planet);
        }
        public static Vector2 Project(Camera camera, Vector3 worldPosition, Vector2 mapSize, float edgeRadius)
        {
            var viewport = camera.WorldToViewportPoint(worldPosition);
            var offset = new Vector2((viewport.x - .5f) * mapSize.x, (viewport.y - .5f) * mapSize.y);
            return Vector2.ClampMagnitude(offset, edgeRadius);
        }
        void LateUpdate()
        {
            if (!state.SceneObjects.TryGet<SpatialSceneObject>(player.SwgId, out _)) return;
            var pose = state.SceneObjects.GetServerWorldPose(player.SwgId);
            var position = pose.position - state.RenderOrigin;
            var rotation = pose.rotation;
            if (ManagedSceneObject.TryGetObjectFrom(player.SwgId, out var body))
            {
                position = body.transform.position;
                rotation = body.transform.rotation;
            }
            playerMarker.localEulerAngles = new Vector3(0, 0, -rotation.eulerAngles.y);
            var serverPosition = position + state.RenderOrigin;
            longitude.text = Mathf.RoundToInt(serverPosition.x).ToString();
            elevation.text = Mathf.RoundToInt(serverPosition.y).ToString();
            latitude.text = Mathf.RoundToInt(serverPosition.z).ToString();
            rangeLabel.text = range.ToString("0") + " m";
            if (Time.unscaledTime >= nextCapture)
            {
                nextCapture = Time.unscaledTime + refreshSeconds;
                capture.orthographicSize = range;
                capture.transform.SetPositionAndRotation(position + Vector3.up * 300, Quaternion.Euler(90, 0, 0));
                capture.Render();
            }
            // Project against the camera that produced the current map image; clamp only at the ring edge.
            var size = bearings.rect.size;
            float radius = Mathf.Min(size.x, size.y) * .40f;
            playerMarker.anchoredPosition = Project(capture, position, size, radius);
            foreach (var item in arrows)
            {
                var destination = item.Waypoint.Position - state.RenderOrigin;
                var delta = item.Waypoint.Position - serverPosition;
                item.Arrow.rectTransform.anchoredPosition = Project(capture, destination, size, radius);
                item.Arrow.rectTransform.localEulerAngles = new Vector3(0, 0, -Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg);
            }
        }
        public void ZoomIn() => SetRange(range * .5f);
        public void ZoomOut() => SetRange(range * 2);
        void SetRange(float value) { range = Mathf.Clamp(value, 16, 1024); nextCapture = 0; }
        public void OnScroll(PointerEventData pointer) { if (pointer.scrollDelta.y > 0) ZoomIn(); else if (pointer.scrollDelta.y < 0) ZoomOut(); }
        void OnDestroy()
        {
            if (capture != null) capture.targetTexture = null;
            if (texture != null) { texture.Release(); Destroy(texture); }
        }
    }
}
