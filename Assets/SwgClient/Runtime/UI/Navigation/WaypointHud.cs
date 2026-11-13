using System.Collections.Generic;
using Swg.Client.Core.World;
using Swg.Client.Core.World.SceneObjects;
using Swg.Client.Runtime.Localization;
using Swg.Client.Runtime.World.SceneObjects;
using UnityEngine;

namespace Swg.Client.Runtime.UI.Navigation
{
    [DefaultExecutionOrder(200)]
    public sealed class WaypointHud : MonoBehaviour, IUiPrefabSource
    {
        sealed class Display
        {
            public Waypoint Waypoint;
            public WaypointTrackerRow Row;
            public NavigationArrow Arrow;
        }

        public Camera worldCamera;
        public GameObject tracker;
        public RectTransform rows;
        public RectTransform arrows;
        public WaypointTrackerRow rowPrefab;
        public NavigationArrow arrowPrefab;
        public IEnumerable<GameObject> UiPrefabs
        {
            get
            {
                yield return rowPrefab.gameObject;
                yield return arrowPrefab.gameObject;
            }
        }
        readonly List<Display> displays = new List<Display>();
        WorldState state;
        StringTableLocalizer localizer;

        void Awake()
        {
            state = GameServices.Resolve<WorldState>();
            localizer = GameServices.Resolve<StringTableLocalizer>();
        }

        void OnEnable()
        {
            if (worldCamera == null)
                worldCamera = Camera.main;
            state.WaypointsChanged += Rebuild;
            state.SceneStarted += Rebuild;
            Rebuild();
        }

        void OnDisable()
        {
            state.WaypointsChanged -= Rebuild;
            state.SceneStarted -= Rebuild;
        }

        void Rebuild()
        {
            foreach (var display in displays)
            {
                display.Row.gameObject.SetActive(false);
                display.Arrow.gameObject.SetActive(false);
                Destroy(display.Row.gameObject);
                Destroy(display.Arrow.gameObject);
            }
            displays.Clear();
            foreach (var waypoint in state.Waypoints.Values)
            {
                if (!waypoint.Active || waypoint.PlanetCrc != state.PlanetCrc)
                    continue;
                var row = Instantiate(rowPrefab, rows);
                var arrow = Instantiate(arrowPrefab, arrows);
                var color = ColorFor(waypoint.Color);
                row.Bind(localizer.Localize(waypoint.Name), color);
                arrow.image.color = color;
                displays.Add(new Display { Waypoint = waypoint, Row = row, Arrow = arrow });
            }
            tracker.SetActive(displays.Count > 0);
        }

        void LateUpdate()
        {
            if (!state.SceneObjects.TryGet<SpatialSceneObject>(state.PlayerSwgId, out _))
                return;
            var playerPosition = state.SceneObjects.GetServerWorldPose(state.PlayerSwgId).position - state.RenderOrigin;
            if (ManagedSceneObject.TryGetObjectFrom(state.PlayerSwgId, out var player))
                playerPosition = player.transform.position;
            foreach (var display in displays)
            {
                var destination = display.Waypoint.Position - state.RenderOrigin;
                var delta = Vector3.ProjectOnPlane(destination - playerPosition, Vector3.up);
                var bearing = worldCamera != null ? NavigationArrow.Bearing(worldCamera, playerPosition, destination) : Vector2.up;
                display.Row.Show(Mathf.RoundToInt(delta.magnitude), bearing);
                if (worldCamera != null)
                    display.Arrow.Show(worldCamera, playerPosition, destination);
            }
        }

        internal static Color ColorFor(WaypointColor color)
        {
            switch (color)
            {
                case WaypointColor.Blue: return new Color32(40, 145, 225, 255);
                case WaypointColor.Green: return new Color32(70, 235, 80, 255);
                case WaypointColor.Orange: return new Color32(255, 145, 40, 255);
                case WaypointColor.Yellow: return new Color32(245, 230, 60, 255);
                case WaypointColor.Purple: return new Color32(195, 100, 245, 255);
                default: return Color.white;
            }
        }
    }
}
