using System;
using UnityEngine;

namespace Swg.Client.Core.World.Terrain.Boundaries
{
    public sealed class RectangleBoundary : Boundary
    {
        readonly Rect bounds;
        public Vector2 FirstCorner { get; }
        public Vector2 SecondCorner { get; }

        public RectangleBoundary(Vector2 firstCorner, Vector2 secondCorner, int featherType, float feather) : base(featherType, feather)
        {
            FirstCorner = firstCorner;
            SecondCorner = secondCorner;
            bounds = Rect.MinMaxRect(Math.Min(firstCorner.x, secondCorner.x), Math.Min(firstCorner.y, secondCorner.y), Math.Max(firstCorner.x, secondCorner.x), Math.Max(firstCorner.y, secondCorner.y));
        }

        protected override float SampleWeight(float x, float z)
        {
            if (!BoundaryGeometry.Contains(bounds, x, z))
                return 0;
            float width = Math.Min(bounds.width, bounds.height) * Math.Min(feather, 1) * .5f;
            return width <= 0 ? 1 : Math.Min(1, Math.Min(Math.Min(x - bounds.xMin, bounds.xMax - x), Math.Min(z - bounds.yMin, bounds.yMax - z)) / width);
        }
    }
}
