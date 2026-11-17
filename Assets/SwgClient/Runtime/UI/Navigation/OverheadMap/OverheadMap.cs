using System.Collections.Generic;
using Swg.Client.Assets.SwgClient.Core.World.SceneObjects.Creature;
using Swg.Client.Core.Characters;
using Swg.Client.Core.World;
using Swg.Client.Core.World.SceneObjects;
using Swg.Client.Runtime.World;
using Swg.Client.Runtime.World.SceneObjects;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Swg.Client.Runtime.UI.Navigation.OverheadMap
{
    /// <summary>A camera-relative overlay of native radar geometry, portal openings and replicated creature bearings.</summary>
    [DefaultExecutionOrder(200)] // Project after the orbit camera and interpolated world poses.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class OverheadMap : MaskableGraphic
    {
        [Min(1)] public float range = 96;
        [Min(.05f)] public float membershipRefreshSeconds = .5f;
        public Color buildingColor = new Color32(55, 145, 0, 185);
        public Color doorColor = new Color32(0, 185, 255, 255);
        public Color neutralColor = new Color32(255, 230, 0, 255);
        public Color friendlyColor = Color.white;
        public Color hostileColor = new Color32(255, 35, 25, 255);
        public Color playerInnerColor = new Color32(25, 115, 255, 255);
        public Vector2 creatureArrowSize = new Vector2(5, 9);
        public Vector2 playerArrowSize = new Vector2(10, 20);
        public float doorWidth = 1.5f;
        public bool IsOpen { get; private set; }

        readonly List<(Vector3[] Vertices, int[] Triangles, Transform Owner)> shapes = new List<(Vector3[], int[], Transform)>();
        readonly List<(Vector3 Start, Vector3 End, Transform Owner)> doors = new List<(Vector3, Vector3, Transform)>();
        readonly List<CreatureSceneObject> creatures = new List<CreatureSceneObject>();
        CurrentPlayer player;
        WorldState state;
        WorldRenderingContext rendering;
        float nextRefresh;
        Vector3 center;
        float cameraYaw;

        protected override void Awake()
        {
            base.Awake();
            if (!Application.isPlaying) return;
            raycastTarget = false;
            player = GameServices.Resolve<CurrentPlayer>();
            state = GameServices.Resolve<WorldState>();
            rendering = GameServices.Resolve<WorldRenderingContext>();
        }

        public void Toggle()
        {
            IsOpen = !IsOpen;
            nextRefresh = 0;
            if (!IsOpen) { shapes.Clear(); doors.Clear(); creatures.Clear(); }
            SetVerticesDirty();
        }

        void LateUpdate()
        {
            if (!Application.isPlaying) return;
            if (Application.isFocused && !UiInput.TextInputFocused && Keyboard.current?.mKey.wasPressedThisFrame == true) Toggle();
            if (!IsOpen) return;
            if (!ManagedSceneObject.TryGetObjectFrom(player.SwgId, out var body))
            {
                shapes.Clear(); doors.Clear(); creatures.Clear(); SetVerticesDirty(); return;
            }
            center = body.transform.position;
            var camera = rendering.WorldCamera;
            cameraYaw = camera == null ? 0 : camera.transform.eulerAngles.y;
            if (Time.unscaledTime >= nextRefresh)
            {
                RefreshMembership();
                nextRefresh = Time.unscaledTime + membershipRefreshSeconds;
            }
            // Only the small authored radar meshes are projected; no scene capture or model meshes.
            SetVerticesDirty();
        }

        void RefreshMembership()
        {
            shapes.Clear(); doors.Clear(); creatures.Clear();
            if (rendering.Renderer != null)
                foreach (var entry in rendering.Renderer.LoadedAppearances)
                {
                    var appearance = entry.Value;
                    if (appearance.RadarShapes.Count == 0 && appearance.PortalLayout == null) continue;
                    if (!state.SceneObjects.TryGet<SpatialSceneObject>(entry.Key, out var sceneObject) || sceneObject is CreatureSceneObject
                        || !ManagedSceneObject.TryGetObjectFrom(entry.Key, out var owner)) continue;
                    float reach = range + appearance.Radius * owner.transform.lossyScale.x;
                    var offset = owner.transform.position - center;
                    if (offset.x * offset.x + offset.z * offset.z > reach * reach) continue;
                    foreach (var shape in appearance.RadarShapes) shapes.Add((shape.Vertices, shape.Triangles, owner.transform));
                    var layout = appearance.PortalLayout;
                    if (layout == null) continue;
                    // Older appearances have no RADR: use their authored room floors, not a guessed bounding box.
                    if (appearance.RadarShapes.Count == 0)
                        foreach (var cell in layout.Cells)
                            if (cell.Number > 0 && cell.Floor != null) shapes.Add((cell.Floor.Vertices, cell.Floor.Triangles, owner.transform));
                    foreach (var entrance in layout.Entrances)
                    {
                        EntranceDash(entrance, out var from, out var to);
                        doors.Add((from, to, owner.transform));
                    }
                    foreach (var door in layout.Doors)
                        doors.Add((door.Transform.MultiplyPoint3x4(Vector3.left), door.Transform.MultiplyPoint3x4(Vector3.right), owner.transform));
                }
            foreach (var sceneObject in state.SceneObjects)
                if (sceneObject is CreatureSceneObject creature && creature.SwgId != player.SwgId
                    && creature.Posture != CreaturePosture.Dead && creature.Mount == null
                    && ManagedSceneObject.TryGetObjectFrom(creature.SwgId, out var body)
                    && body.TryGetComponent<TargetableSceneObject>(out _)) creatures.Add(creature);
        }

        internal static Vector2 Project(Vector3 position, Vector3 origin, float yawDegrees, float pixelsPerMeter)
        {
            var delta = position - origin;
            float yaw = yawDegrees * Mathf.Deg2Rad;
            return new Vector2(delta.x * Mathf.Cos(yaw) - delta.z * Mathf.Sin(yaw), delta.x * Mathf.Sin(yaw) + delta.z * Mathf.Cos(yaw)) * pixelsPerMeter;
        }

        internal Color ColorFor(CreatureSceneObject creature) => !creature.CanAttack ? friendlyColor : creature.IsHostile ? hostileColor : neutralColor;

        internal static void EntranceDash(Vector3[] polygon, out Vector3 from, out Vector3 to)
        {
            from = to = polygon[0];
            float longest = 0;
            foreach (var a in polygon)
                foreach (var b in polygon)
                {
                    float distance = (new Vector2(a.x, a.z) - new Vector2(b.x, b.z)).sqrMagnitude;
                    if (distance <= longest) continue;
                    longest = distance; from = a; to = b;
                }
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (!IsOpen || player == null || !ManagedSceneObject.TryGetObjectFrom(player.SwgId, out var body)) return;
            float scale = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) / (2 * range);
            float sine = Mathf.Sin(cameraYaw * Mathf.Deg2Rad);
            float cosine = Mathf.Cos(cameraYaw * Mathf.Deg2Rad);
            var mapCenter = rectTransform.rect.center;
            Vector2 ProjectPoint(Vector3 point)
            {
                var delta = point - center;
                return new Vector2(delta.x * cosine - delta.z * sine, delta.x * sine + delta.z * cosine) * scale + mapCenter;
            }
            foreach (var shape in shapes)
            {
                if (shape.Owner == null) continue;
                int first = mesh.currentVertCount;
                var transform = shape.Owner.localToWorldMatrix;
                foreach (var point in shape.Vertices) mesh.AddVert(ProjectPoint(transform.MultiplyPoint3x4(point)), buildingColor, Vector2.zero);
                for (int i = 0; i < shape.Triangles.Length; i += 3)
                    mesh.AddTriangle(first + shape.Triangles[i], first + shape.Triangles[i + 1], first + shape.Triangles[i + 2]);
            }
            foreach (var door in doors)
                if (door.Owner != null) Line(mesh, ProjectPoint(door.Owner.TransformPoint(door.Start)), ProjectPoint(door.Owner.TransformPoint(door.End)), doorWidth, doorColor);
            foreach (var creature in creatures)
            {
                if (!ManagedSceneObject.TryGetObjectFrom(creature.SwgId, out var owner)) continue;
                var point = ProjectPoint(owner.transform.position);
                if (!rectTransform.rect.Contains(point)) continue;
                var facing = Project(owner.transform.forward, Vector3.zero, cameraYaw, 1).normalized;
                Arrow(mesh, point, facing, creatureArrowSize, ColorFor(creature));
            }
            var direction = Project(body.transform.forward, Vector3.zero, cameraYaw, 1).normalized;
            var playerPoint = ProjectPoint(body.transform.position);
            Arrow(mesh, playerPoint, direction, playerArrowSize, Color.white);
            Arrow(mesh, playerPoint, direction, playerArrowSize * .62f, playerInnerColor);
        }

        static void Arrow(VertexHelper mesh, Vector2 point, Vector2 forward, Vector2 size, Color tint)
        {
            var right = new Vector2(forward.y, -forward.x);
            int start = mesh.currentVertCount;
            mesh.AddVert(point + forward * (size.y * .5f), tint, Vector2.zero);
            mesh.AddVert(point - forward * (size.y * .5f) - right * (size.x * .5f), tint, Vector2.zero);
            mesh.AddVert(point - forward * (size.y * .2f), tint, Vector2.zero);
            mesh.AddVert(point - forward * (size.y * .5f) + right * (size.x * .5f), tint, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2);
            mesh.AddTriangle(start, start + 2, start + 3);
        }

        static void Line(VertexHelper mesh, Vector2 a, Vector2 b, float width, Color tint)
        {
            var delta = (b - a).normalized * (width * .5f);
            var normal = new Vector2(-delta.y, delta.x);
            int start = mesh.currentVertCount;
            mesh.AddVert(a - normal, tint, Vector2.zero); mesh.AddVert(a + normal, tint, Vector2.zero);
            mesh.AddVert(b + normal, tint, Vector2.zero); mesh.AddVert(b - normal, tint, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2); mesh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
