using System;
using UnityEngine;

namespace Swg.Client.Core.World.Terrain.Boundaries
{
    public sealed class CircleBoundary : Boundary
    {
        readonly Vector2 center;
        readonly float radius;
        public Vector2 Center => center;
        public float Radius => radius;

        public CircleBoundary(Vector2 center, float radius, int featherType, float feather) : base(featherType, feather)
        {
            this.center = center;
            this.radius = radius;
        }

        protected override float SampleWeight(float x, float z)
        {
            float dx = x - center.x;
            float dz = z - center.y;
            float distance = dx * dx + dz * dz;
            if (distance > radius * radius)
                return 0;
            float inner = radius * (1 - Math.Min(feather, 1));
            return distance <= inner * inner ? 1 : 1 - (distance - inner * inner) / (radius * radius - inner * inner);
        }
    }
}
