using Swg.Client.Runtime.World.Terrain;
using System.Collections.Generic;
using System.Linq;
using Swg.Client.Core;
using Swg.Client.Core.Characters;
using Swg.Client.Core.World;
using Swg.Client.Core.World.SceneObjects;
using Swg.Client.Core.Assets.Formats.Pob;
using UnityEngine;
using UnityEngine.Rendering;

namespace Swg.Client.Runtime.World.SceneObjects
{
    /// <summary>Native interior visibility, room floor colliders and mounted-entry barriers. Room IDs are resolved from replicated cells.</summary>
    public sealed class BuildingInterior : MonoBehaviour
    {
        internal const string ExteriorCellName = "Exterior cell";
        internal static string CellRootName(int number) => number == 0 ? ExteriorCellName : "Interior cell " + number;
        readonly Dictionary<int, InteriorLighting> lighting = new Dictionary<int, InteriorLighting>();
        public SwgId BuildingSwgId { get; private set; }
        ExteriorPortalVisibility exterior;
        Camera worldCamera;
        bool playerInside;
        readonly List<Mesh> meshes = new List<Mesh>();
        readonly List<GameObject> barriers = new List<GameObject>();
        WorldState state;
        CurrentPlayer player;
        Material barrierMaterial;
        WorldTerrainRenderer terrain;
        public void Initialize(SwgId swgId, PortalLayout layout)
        {
            BuildingSwgId = swgId;
            var exteriorRoot = transform.Find(ExteriorCellName);
            if (exteriorRoot != null)
                exterior = new ExteriorPortalVisibility(exteriorRoot.gameObject, layout.Entrances, transform.localToWorldMatrix);
            state = GameServices.Resolve<WorldState>();
            player = GameServices.Resolve<CurrentPlayer>();
            foreach (var cell in layout.Cells)
            {
                var floor = cell.Floor;
                if (floor == null) continue;
                var child = new GameObject("Room floor " + cell.Number);
                child.transform.SetParent(transform, false);
                var mesh = new Mesh { name = child.name, vertices = floor.Vertices, triangles = floor.Triangles };
                meshes.Add(mesh);
                // Floor winding differs across assets; duplicate the back face for reliable grounding.
                var indices = mesh.triangles;
                mesh.triangles = indices.Concat(indices.Reverse()).ToArray();
                child.AddComponent<MeshCollider>().sharedMesh = mesh;
                var room = child.AddComponent<BuildingFloor>();
                room.building = this;
                room.cellNumber = cell.Number;
            }
            // Share positional lights so illumination falls off across doorways instead of stopping at a cell boundary.
            var pointLights = layout.Cells.SelectMany(cell => cell.Lights).Where(light => light.Type == PortalLayout.LightType.Point).ToArray();
            foreach (var cell in layout.Cells)
            {
                if (cell.Number == 0) continue;
                var roomLighting = new InteriorLighting(cell, transform.localToWorldMatrix, pointLights);
                lighting.Add(cell.Number, roomLighting);
                var room = transform.Find(CellRootName(cell.Number));
                if (room != null) roomLighting.Apply(room.gameObject);
                foreach (var occupant in state.SceneObjects.ChildrenOf(CellSwgId(cell.Number)))
                    if (ManagedSceneObject.TryGetObjectFrom(occupant.SwgId, out var body)) roomLighting.Apply(body);
            }
            terrain = GameServices.Resolve<WorldTerrainRenderer>();
            terrain.RegisterInterior(this);
            var rendering = GameServices.Resolve<WorldRenderingContext>();
            worldCamera = rendering.WorldCamera;
            barrierMaterial = new Material(rendering.ParticleShader);
            foreach (var polygon in layout.Entrances)
            {
                if (polygon.Length < 3) continue;
                var child = new GameObject("Mounted entry barrier");
                child.transform.SetParent(transform, false);
                var indices = new List<int>();
                // Portals are shared by two cells; their native winding need not face outdoors.
                // Block mounted entry from either side, independently of that winding.
                for (int i = 2; i < polygon.Length; i++)
                {
                    indices.Add(0); indices.Add(i - 1); indices.Add(i);
                    indices.Add(0); indices.Add(i); indices.Add(i - 1);
                }
                var mesh = new Mesh { name = child.name, vertices = polygon, triangles = indices.ToArray(), colors = polygon.Select(_ => new Color(1, .03f, .01f, .24f)).ToArray() };
                meshes.Add(mesh);
                child.AddComponent<MeshFilter>().sharedMesh = mesh;
                child.AddComponent<MeshRenderer>().sharedMaterial = barrierMaterial;
                child.AddComponent<MeshCollider>().sharedMesh = mesh;
                barriers.Add(child);
                child.SetActive(false);
            }
            RenderPipelineManager.beginCameraRendering += BeforeCamera;
            state.SceneObjects.Subscribe(player.SwgId, OnPlayerChanged);
            OnPlayerChanged(SceneObjectChange.Containment);
        }
        public SwgId CellSwgId(int number)
        {
            if (number == 0) return default;
            foreach (var cell in state.SceneObjects.ChildrenOf(BuildingSwgId).OfType<CellSceneObject>())
                if (cell.Number == number) return cell.SwgId;
            if (state.Snapshot != null)
                foreach (var cell in state.Snapshot.Entries)
                    if (cell.ParentSwgId == BuildingSwgId && cell.CellSwgId == number) return cell.SwgId;
            return default;
        }
        internal static void ApplyLighting(SpatialSceneObject sceneObject, GameObject body)
        {
            if (body.GetComponent<BuildingInterior>() != null) return;
            if (sceneObject.Parent is CellSceneObject cell && ManagedSceneObject.TryGetObjectFrom(cell.ParentSwgId, out var building)
                && building.TryGetComponent<BuildingInterior>(out var interior) && interior.lighting.TryGetValue(cell.Number, out var roomLighting))
                roomLighting.Apply(body);
            else InteriorLighting.Reset(body);
        }

        void OnPlayerChanged(SceneObjectChange changes)
        {
            if ((changes & SceneObjectChange.Containment) == 0) return;
            // Keep the outside apron visible through the doors, without drawing the exterior
            // shell across the rooms. This must not depend on crossing out of the current cell.
            playerInside = player.Creature?.Parent is CellSceneObject cell && cell.ParentSwgId == BuildingSwgId;
            exterior?.SetInside(playerInside);
            bool mounted = player.Creature?.Mount != null;
            foreach (var barrier in barriers) barrier.SetActive(mounted);
        }
        void BeforeCamera(ScriptableRenderContext context, Camera camera)
        {
            if (!playerInside || camera != worldCamera || exterior == null) return;
            // The orbit camera can pass through an entrance while the player stays indoors.
            // Use its final pose immediately before rendering, after WorldOrbitCamera.LateUpdate.
            var interiorPoint = ManagedSceneObject.CurrentPlayer.transform.position + Vector3.up * 1.4f;
            exterior.UpdateCamera(camera.transform.position, interiorPoint);
        }

        void OnDestroy()
        {
            RenderPipelineManager.beginCameraRendering -= BeforeCamera;
            state?.SceneObjects.Unsubscribe(player.SwgId, OnPlayerChanged);
            foreach (var mesh in meshes) Destroy(mesh);
            if (barrierMaterial != null) Destroy(barrierMaterial);
            terrain?.RemoveInterior(this);
            exterior?.Dispose();
        }
    }
}
