using System.Collections.Generic;
using Swg.Client.Core;
using Swg.Client.Core.World;
using UnityEngine;

namespace Swg.Client.Runtime.World.SceneObjects
{
    /// <summary>Displays authored waypoint particle pillars; the HUD independently renders navigation arrows and distances.</summary>
    public sealed class WorldWaypointRenderer : MonoBehaviour
    {
        public ParticleSystem pillarPrefab;
        [Min(1)]
        public float visibilityDistance = 512;
        readonly Dictionary<SwgId, GameObject> pillars = new Dictionary<SwgId, GameObject>();
        WorldState state;

        void OnEnable()
        {
            state = GameServices.Resolve<WorldState>();
            state.WaypointsChanged += Rebuild;
            state.SceneStarted += Rebuild;
            Rebuild();
        }

        void Rebuild()
        {
            ClearPillars();
            foreach (var waypoint in state.Waypoints.Values)
            {
                if (!waypoint.Active || waypoint.PlanetCrc != state.PlanetCrc)
                    continue;
                var position = waypoint.Position;
                // Most ground waypoints are transmitted at height zero; place their pillar on the terrain.
                if (state.Terrain != null)
                    position.y = state.Terrain.SampleHeight(position.x, position.z);
                var instance = Instantiate(pillarPrefab, transform.TransformPoint(position - state.RenderOrigin), Quaternion.identity, transform);
                var pillar = instance.gameObject;
                pillar.name = waypoint.Name + " (" + waypoint.SwgId + ")";
                pillars.Add(waypoint.SwgId, pillar);
            }
            UpdateVisibility();
        }

        void LateUpdate() => UpdateVisibility();

        void UpdateVisibility()
        {
            if (!ManagedSceneObject.TryGetObjectFrom(state.PlayerSwgId, out var player))
                return;
            float distanceSquared = visibilityDistance * visibilityDistance;
            foreach (var pillar in pillars.Values)
                pillar.SetActive((pillar.transform.position - player.transform.position).sqrMagnitude <= distanceSquared);
        }

        void ClearPillars()
        {
            foreach (var pillar in pillars.Values)
            {
                pillar.SetActive(false);
                Destroy(pillar);
            }
            pillars.Clear();
        }

        void OnDisable()
        {
            state.WaypointsChanged -= Rebuild;
            state.SceneStarted -= Rebuild;
            ClearPillars();
        }

    }
}
