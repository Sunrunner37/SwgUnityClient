using System.Collections.Generic;
using System.Linq;
using Swg.Client.Core;
using Swg.Client.Core.Characters;
using Swg.Client.Core.World;
using Swg.Client.Core.World.SceneObjects;
using Swg.Client.Core.Assets.Formats.Pob;
using UnityEngine;

namespace Swg.Client.Runtime.World.SceneObjects
{
    /// <summary>Native interior visibility, room floor colliders and mounted-entry barriers. Room IDs are resolved from replicated cells.</summary>
    public sealed class BuildingInterior : MonoBehaviour
    {
        internal const string ExteriorCellName = "Exterior cell";
        public SwgId BuildingSwgId { get; private set; }
        GameObject exterior;
        readonly List<Mesh> meshes = new List<Mesh>();
        readonly List<GameObject> barriers = new List<GameObject>();
        WorldState state;
        CurrentPlayer player;
        Material barrierMaterial;
        public void Initialize(SwgId swgId, PortalLayout layout)
        {
            BuildingSwgId = swgId;
            exterior = transform.Find(ExteriorCellName)?.gameObject;
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
            barrierMaterial = new Material(GameServices.Resolve<WorldRenderingContext>().ParticleShader);
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
        void OnPlayerChanged(SceneObjectChange changes)
        {
            if ((changes & SceneObjectChange.Containment) == 0) return;
            // Native exterior cells can contain closed shell geometry that overlaps the rooms.
            // Keep it out of both rendering and collision while occupying this building's interior.
            bool inside = player.Creature?.Parent is CellSceneObject cell && cell.ParentSwgId == BuildingSwgId;
            if (exterior != null) exterior.SetActive(!inside);
            bool mounted = player.Creature?.Mount != null;
            foreach (var barrier in barriers) barrier.SetActive(mounted);
        }
        void OnDestroy()
        {
            state?.SceneObjects.Unsubscribe(player.SwgId, OnPlayerChanged);
            foreach (var mesh in meshes) Destroy(mesh);
            if (barrierMaterial != null) Destroy(barrierMaterial);
        }
    }
}
