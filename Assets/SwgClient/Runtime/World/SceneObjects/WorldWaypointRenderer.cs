using Swg.Client.Core.Characters;
using System.Collections.Generic;
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
        readonly List<GameObject> pillars = new List<GameObject>();
        WorldState state;
        CurrentPlayer currentPlayer;

        void OnEnable()
        {
            currentPlayer = GameServices.Resolve<CurrentPlayer>();
            state = GameServices.Resolve<WorldState>();
            currentPlayer.WaypointsChanged += Rebuild;
            state.SceneStarted += Rebuild;
            Rebuild();
        }

        void Rebuild()
        {
            ClearPillars();
            foreach (var waypoint in currentPlayer.NavigationWaypoints)
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
                pillars.Add(pillar);
            }
            UpdateVisibility();
        }

        void LateUpdate() => UpdateVisibility();

        void UpdateVisibility()
        {
            if (!ManagedSceneObject.TryGetObjectFrom(currentPlayer.SwgId, out var player))
                return;
            float distanceSquared = visibilityDistance * visibilityDistance;
            foreach (var pillar in pillars)
                pillar.SetActive((pillar.transform.position - player.transform.position).sqrMagnitude <= distanceSquared);
        }

        void ClearPillars()
        {
            foreach (var pillar in pillars)
            {
                pillar.SetActive(false);
                Destroy(pillar);
            }
            pillars.Clear();
        }

        void OnDisable()
        {
            currentPlayer.WaypointsChanged -= Rebuild;
            state.SceneStarted -= Rebuild;
            ClearPillars();
        }

    }
}
