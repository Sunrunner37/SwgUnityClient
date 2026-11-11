using System;
using System.Collections.Generic;
using UnityEngine;

namespace Swg.Client.Runtime.World.Streaming
{
    public static class WorldRegionPlanner
    {
        public static IEnumerable<Vector2Int> TerrainKeys(Vector3 serverCenter, float radius, float tileSize)
        {
            if (radius <= 0 || float.IsNaN(radius) || float.IsInfinity(radius) || tileSize <= 0 || float.IsNaN(tileSize) || float.IsInfinity(tileSize))
                throw new ArgumentOutOfRangeException(nameof(radius));
            int minX = Mathf.FloorToInt((serverCenter.x - radius) / tileSize);
            int minZ = Mathf.FloorToInt((serverCenter.z - radius) / tileSize);
            int maxX = Mathf.FloorToInt((serverCenter.x + radius) / tileSize);
            int maxZ = Mathf.FloorToInt((serverCenter.z + radius) / tileSize);
            for (int z = minZ; z <= maxZ; z++)
                for (int x = minX; x <= maxX; x++)
                    yield return new Vector2Int(x, z);
        }
    }
}
