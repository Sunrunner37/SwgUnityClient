using System;
using System.Linq;
using Swg.Client.Core.Assets.Appearances;
using Swg.Client.Core;
using UnityEngine;

namespace Swg.Client.Runtime.World.SceneObjects
{
    /// <summary>Cached appearance bounds for picking, with upper-body selection corners for creatures.</summary>
    [DisallowMultipleComponent]
    public sealed class TargetableSceneObject : MonoBehaviour
    {
        public const string LayerName = "SWG Targets";
        public SwgId SwgId { get; private set; }
        public Bounds LocalBounds { get; private set; }

        public Bounds SelectionBounds { get; private set; }
        internal Vector3[][] SelectionGeometry { get; private set; } = Array.Empty<Vector3[]>();

        public void Initialize(SwgId swgId, AppearanceData appearance, bool creature)
        {
            SwgId = swgId;
            var appearanceBounds = new Bounds((appearance.BoundsMin + appearance.BoundsMax) * 0.5f, appearance.BoundsMax - appearance.BoundsMin);
            // Share the cached geometry; do not copy vertices or include particle attachments.
            if (!creature)
                SelectionGeometry = appearance.Parts.Select(part => part.Vertices).ToArray();
            LocalBounds = appearanceBounds;
            SelectionBounds = creature
                ? new Bounds(appearanceBounds.center + Vector3.up * appearanceBounds.size.y * 0.25f,
                    Vector3.Scale(appearanceBounds.size, new Vector3(0.65f, 0.5f, 0.65f)))
                : appearanceBounds;
            if (appearanceBounds.size == Vector3.zero)
                return;
            var volume = new GameObject("Target pick volume");
            volume.transform.SetParent(transform, false);
            volume.layer = LayerMask.NameToLayer(LayerName);
            var collider = volume.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.center = appearanceBounds.center;
            collider.size = Vector3.Max(appearanceBounds.size, Vector3.one * 0.15f);
        }

        public static Vector3 Corner(Bounds bounds, int index) => bounds.center + Vector3.Scale(bounds.extents,
            new Vector3((index & 1) == 0 ? -1 : 1, (index & 2) == 0 ? -1 : 1, (index & 4) == 0 ? -1 : 1));
    }
}
