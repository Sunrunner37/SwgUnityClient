using System;
using UnityEngine;

namespace Swg.Client.Core.World.Terrain.Boundaries
{
    public sealed class PolygonBoundary : Boundary
    {
        readonly Vector2[] vertices;
        readonly Rect bounds;
        public Vector2[] Vertices => vertices;

        public PolygonBoundary(Vector2[] vertices, int featherType, float feather) : base(featherType, feather)
        {
            this.vertices = vertices;
            bounds = BoundaryGeometry.Bounds(vertices);
        }

        protected override float SampleWeight(float x, float z)
        {
            if (!BoundaryGeometry.Contains(bounds, x, z))
                return 0;
            bool inside = false;
            float distance = float.MaxValue;
            for (int i = 0; i < vertices.Length; i++)
            {
                var a = vertices[i];
                var b = vertices[(i + 1) % vertices.Length];
                if ((a.y > z) != (b.y > z) && x < (b.x - a.x) * (z - a.y) / (b.y - a.y) + a.x)
                    inside = !inside;
                distance = Math.Min(distance, BoundaryGeometry.SegmentDistanceSquared(x, z, a, b));
            }

            if (!inside)
                return 0;
            return feather <= 0 ? 1 : Math.Min(1, (float)Math.Sqrt(distance) / feather);
        }
    }
}
