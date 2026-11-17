using System;
using System.Collections.Generic;
using UnityEngine;

namespace Swg.Client.Runtime.World.SceneObjects
{
    /// <summary>Shows a building's exterior through its native entrances while keeping overlapping shell geometry out of the rooms.</summary>
    internal sealed class ExteriorPortalVisibility : IDisposable
    {
        readonly Renderer[] renderers;
        readonly Collider[] colliders;
        readonly Material[][] originals;
        Material[][] clipped;
        readonly List<Material> ownedMaterials = new List<Material>();
        readonly ComputeBuffer portals;
        readonly Vector4[] portalVertices;
        bool? viewInside;
        readonly int triangleCount;

        public ExteriorPortalVisibility(GameObject exterior, IEnumerable<Vector3[]> entrances, Matrix4x4 building)
        {
            renderers = exterior.GetComponentsInChildren<Renderer>(true);
            colliders = exterior.GetComponentsInChildren<Collider>(true);
            originals = new Material[renderers.Length][];
            for (int i = 0; i < renderers.Length; i++) originals[i] = renderers[i].sharedMaterials;
            var vertices = new List<Vector4>();
            foreach (var polygon in entrances)
                for (int i = 2; i < polygon.Length; i++)
                {
                    vertices.Add(building.MultiplyPoint3x4(polygon[0]));
                    vertices.Add(building.MultiplyPoint3x4(polygon[i - 1]));
                    vertices.Add(building.MultiplyPoint3x4(polygon[i]));
                }
            portalVertices = vertices.ToArray();
            triangleCount = vertices.Count / 3;
            // A buffer keeps the authored portal count unrestricted; it is uploaded once per building.
            portals = new ComputeBuffer(Math.Max(1, vertices.Count), sizeof(float) * 4);
            if (vertices.Count > 0) portals.SetData(vertices);
        }

        public void SetInside(bool inside)
        {
            SetViewInside(inside);
            // Collision follows the player, independently of the orbit camera's cell.
            foreach (var collider in colliders) collider.enabled = !inside;
        }

        public void UpdateCamera(Vector3 cameraPosition, Vector3 interiorPoint)
        {
            var ray = cameraPosition - interiorPoint;
            for (int i = 0; i < portalVertices.Length; i += 3)
            {
                Vector3 a = portalVertices[i];
                Vector3 edge1 = (Vector3)portalVertices[i + 1] - a;
                Vector3 edge2 = (Vector3)portalVertices[i + 2] - a;
                var p = Vector3.Cross(ray, edge2);
                float determinant = Vector3.Dot(edge1, p);
                if (Mathf.Abs(determinant) < 1e-6f) continue;
                var offset = interiorPoint - a;
                float u = Vector3.Dot(offset, p) / determinant;
                var q = Vector3.Cross(offset, edge1);
                float v = Vector3.Dot(ray, q) / determinant;
                float t = Vector3.Dot(edge2, q) / determinant;
                if (u >= -1e-4f && v >= -1e-4f && u + v <= 1.0001f && t >= 0 && t <= 1.0001f)
                {
                    SetViewInside(false);
                    return;
                }
            }
            SetViewInside(true);
        }

        void SetViewInside(bool inside)
        {
            if (viewInside == inside) return;
            viewInside = inside;
            if (inside && clipped == null)
            {
                clipped = new Material[renderers.Length][];
                var copies = new Dictionary<Material, Material>();
                for (int i = 0; i < renderers.Length; i++)
                {
                    clipped[i] = new Material[originals[i].Length];
                    for (int j = 0; j < originals[i].Length; j++)
                    {
                        var source = originals[i][j];
                        if (!copies.TryGetValue(source, out var copy))
                        {
                            copy = new Material(source);
                            copy.EnableKeyword("SWG_EXTERIOR_PORTALS");
                            copies.Add(source, copy);
                            ownedMaterials.Add(copy);
                        }
                        clipped[i][j] = copy;
                    }
                }
            }
            for (int i = 0; i < renderers.Length; i++) renderers[i].sharedMaterials = inside ? clipped[i] : originals[i];
            var properties = new MaterialPropertyBlock();
            foreach (var renderer in renderers)
            {
                renderer.GetPropertyBlock(properties);
                properties.SetFloat("_SwgExteriorPortalClip", inside ? 1 : 0);
                properties.SetInt("_SwgExteriorPortalCount", triangleCount);
                properties.SetBuffer("_SwgExteriorPortals", portals);
                renderer.SetPropertyBlock(properties);
            }
        }

        public void Dispose()
        {
            portals.Dispose();
            foreach (var material in ownedMaterials)
                if (Application.isPlaying) UnityEngine.Object.Destroy(material);
                else UnityEngine.Object.DestroyImmediate(material);
        }
    }
}
