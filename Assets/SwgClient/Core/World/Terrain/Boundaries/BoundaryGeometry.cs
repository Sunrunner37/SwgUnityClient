using System;
using UnityEngine;

namespace Swg.Client.Core.World.Terrain.Boundaries
{
    internal static class BoundaryGeometry
    {
        public static Rect Bounds(Vector2[] vertices)
        {
            var minimum = vertices[0];
            var maximum = vertices[0];
            foreach (var vertex in vertices)
            {
                minimum = Vector2.Min(minimum, vertex);
                maximum = Vector2.Max(maximum, vertex);
            }

            return Rect.MinMaxRect(minimum.x, minimum.y, maximum.x, maximum.y);
        }

        public static bool Contains(Rect bounds, float x, float z) => x >= bounds.xMin && x <= bounds.xMax && z >= bounds.yMin && z <= bounds.yMax;
        public static float SegmentDistanceSquared(float x, float z, Vector2 a, Vector2 b)
        {
            float dx = b.x - a.x;
            float dz = b.y - a.y;
            float length = dx * dx + dz * dz;
            float t = length == 0 ? 0 : Math.Max(0, Math.Min(1, ((x - a.x) * dx + (z - a.y) * dz) / length));
            float offsetX = x - a.x - dx * t;
            float offsetZ = z - a.y - dz * t;
            return offsetX * offsetX + offsetZ * offsetZ;
        }
    }
}
