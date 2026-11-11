using System;
using System.Collections.Generic;
using UnityEngine;

namespace Swg.Client.Core.World.Terrain
{
    /// <summary>Spatial lookup for native clearance circles; avoids testing every building for each grass instance.</summary>
    public sealed class FloraClearanceIndex
    {
        const float CellSize = 32;
        readonly Dictionary<(int X, int Z), List<FloraClearance>> cells = new Dictionary<(int, int), List<FloraClearance>>();
        public FloraClearanceIndex(IEnumerable<FloraClearance> circles)
        {
            foreach (var circle in circles)
                for (int z = Cell(circle.Center.z - circle.Radius); z <= Cell(circle.Center.z + circle.Radius); z++)
                    for (int x = Cell(circle.Center.x - circle.Radius); x <= Cell(circle.Center.x + circle.Radius); x++)
                    {
                        if (!cells.TryGetValue((x, z), out var list))
                            cells.Add((x, z), list = new List<FloraClearance>());
                        list.Add(circle);
                    }
        }

        public bool Contains(Vector3 point, bool collidable = false)
        {
            if (cells.TryGetValue((Cell(point.x), Cell(point.z)), out var circles))
                foreach (var circle in circles)
                    if (circle.Collidable == collidable && circle.Contains(point))
                        return true;
            return false;
        }

        static int Cell(float coordinate) => (int)Math.Floor(coordinate / CellSize);
    }
}
