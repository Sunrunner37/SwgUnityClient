using Swg.Client.Runtime.World.Terrain;
using Swg.Client.Core;
using Swg.Client.Core.World;
using Swg.Client.Core.World.SceneObjects;
using UnityEngine;

namespace Swg.Client.Runtime.World.SceneObjects
{
    /// <summary>Owns the controlled body's capsule, grounding and native room-floor queries. Positions returned are Unity world coordinates.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(CharacterController))]
    public sealed class PlayerCollision : MonoBehaviour
    {
        readonly RaycastHit[] hits = new RaycastHit[64];
        CharacterController controller;
        WorldState state;
        bool mounted;
        int mask;
        public SwgId CellSwgId { get; private set; }

        public void Initialize(WorldState state, bool mounted)
        {
            this.state = state;
            this.mounted = mounted;
            controller = GetComponent<CharacterController>();
            controller.height = mounted ? 1.6f : 1.7f;
            controller.radius = mounted ? .65f : .25f;
            controller.center = Vector3.up * (controller.height * .5f + .03f);
            controller.skinWidth = .025f;
            // Terrain is traversable; its uphill cost is applied to movement speed.
            // The capsule still blocks vertical walls and building entrances.
            controller.slopeLimit = 90;
            mask = ~(1 << LayerMask.NameToLayer(TargetableSceneObject.LayerName));
            enabled = true;
            controller.enabled = true;
        }

        void OnEnable() { if (controller != null) controller.enabled = true; }
        void OnDisable() { if (controller != null) controller.enabled = false; }

        public Vector3 Move(Vector3 heading, float distance, float seconds)
        {
            Physics.SyncTransforms();
            float step = mounted ? .1f : .45f;
            int count = Physics.RaycastNonAlloc(transform.position + heading * (distance + controller.radius) + Vector3.up * 1.25f,
                Vector3.down, hits, controller.height * transform.lossyScale.y + 1.25f, mask, QueryTriggerInteraction.Ignore);
            var upcomingFloor = FindBuildingFloor(hits, count);
            if (!mounted && upcomingFloor != null)
                step = 1.2f;
            // Native room floors can sit below the outdoor terrain. Let the capsule reach
            // that floor rather than reporting terrain height as a valid interior position.
            controller.excludeLayers = !mounted && upcomingFloor != null && upcomingFloor.cellNumber > 0
                ? 1 << LayerMask.NameToLayer(WorldTerrainRenderer.LayerName) : 0;
            controller.stepOffset = Mathf.Min(controller.height - .05f, step / transform.lossyScale.y);
            var previousPosition = transform.position;
            controller.Move(heading * distance + Vector3.down * (9.81f * seconds));

            // Room membership is independent of the prop or stair tread supporting the capsule.
            count = Physics.RaycastNonAlloc(transform.position + Vector3.up * .6f, Vector3.down, hits, controller.height * transform.lossyScale.y + .6f, mask, QueryTriggerInteraction.Ignore);
            var floor = FindBuildingFloor(hits, count);
            // Core3 forbids mounted cell entry. Never predict an interior pose as outdoor movement,
            // even if a missing portal barrier or a large step lets the capsule cross the doorway.
            var destinationCell = !mounted && floor != null ? floor.building.CellSwgId(floor.cellNumber) : default;
            bool denied = state.SceneObjects.TryGet<CellSceneObject>(destinationCell, out var cell) && !cell.AllowEntry;
            if (denied || mounted && floor != null && floor.cellNumber > 0)
            {
                controller.enabled = false;
                transform.position = previousPosition;
                controller.enabled = true;
                return previousPosition;
            }
            CellSwgId = destinationCell;
            if (floor == null && state.Terrain != null)
            {
                var serverPosition = transform.position + state.RenderOrigin;
                float ground = state.Terrain.SampleHeight(serverPosition.x, serverPosition.z) - state.RenderOrigin.y;
                if (transform.position.y < ground)
                    transform.position = new Vector3(transform.position.x, ground, transform.position.z);
            }
            return transform.position;
        }

        public float TerrainSpeed(Vector3 heading, float angleRadians, float uphillMultiplier)
        {
            // Sample the rendered terrain collider, not the procedural generator every frame.
            if (!CellSwgId.IsNone || !Physics.Raycast(transform.position + Vector3.up * .6f,
                    Vector3.down, out var ground, 2f, mask, QueryTriggerInteraction.Ignore)
                || ground.collider.gameObject.layer != LayerMask.NameToLayer(WorldTerrainRenderer.LayerName))
                return 1;
            return SlopeSpeed(ground.normal, heading, angleRadians, uphillMultiplier);
        }

        internal static float SlopeSpeed(Vector3 normal, Vector3 heading, float angleRadians, float uphillMultiplier)
        {
            // Native CreatureController: interpolate from normal speed at the tolerance
            // to the server's negotiated multiplier on a vertical uphill face.
            float threshold = -Mathf.Sin(angleRadians);
            float slope = Vector3.Dot(normal, heading);
            if (slope >= threshold || threshold <= -1) return 1;
            return Mathf.Lerp(1, Mathf.Clamp01(uphillMultiplier), (slope - threshold) / (-1 - threshold));
        }

        internal static BuildingFloor FindBuildingFloor(RaycastHit[] hits, int count)
        {
            BuildingFloor floor = null;
            float closest = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                if (!hits[i].collider.TryGetComponent<BuildingFloor>(out var candidate))
                    continue;
                if (hits[i].distance < closest - .02f || Mathf.Abs(hits[i].distance - closest) <= .02f && candidate.cellNumber > 0 && (floor == null || floor.cellNumber == 0))
                {
                    closest = hits[i].distance;
                    floor = candidate;
                }
            }
            return floor;
        }
    }
}
