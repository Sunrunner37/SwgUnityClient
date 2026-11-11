using System;
using UnityEngine;

namespace Swg.Client.Core.World.Terrain.Boundaries
{
    public sealed class PolylineBoundary : Boundary
    {
        readonly Vector2[] vertices;
        readonly float radius;
        readonly Rect bounds;
        public Vector2[] Vertices => vertices;
        public float Radius => radius;

        public PolylineBoundary(Vector2[] vertices, float radius, int featherType, float feather) : base(featherType, feather)
        {
            this.vertices = vertices;
            this.radius = radius;
            var extent = BoundaryGeometry.Bounds(vertices);
            bounds = Rect.MinMaxRect(extent.xMin - radius, extent.yMin - radius, extent.xMax + radius, extent.yMax + radius);
        }

        protected override float SampleWeight(float x, float z)
        {
            if (!BoundaryGeometry.Contains(bounds, x, z))
                return 0;
            float distance = float.MaxValue;
            for (int i = 0; i < vertices.Length - 1; i++)
                distance = Math.Min(distance, BoundaryGeometry.SegmentDistanceSquared(x, z, vertices[i], vertices[i + 1]));
            if (distance >= radius * radius)
                return 0;
            float inner = radius * (1 - Math.Min(feather, 1));
            return distance <= inner * inner ? 1 : 1 - ((float)Math.Sqrt(distance) - inner) / (radius - inner);
        }
    }
}
