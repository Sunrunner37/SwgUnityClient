using Swg.Client.Core;
using Swg.Client.Core.Assets.Models;
using System.Collections.Generic;
using Swg.Client.Core.World.Terrain;
using UnityEngine;

namespace Swg.Client.Runtime.World
{
    /// <summary>A region delta: desired residency plus only the terrain and scenery not already loaded.</summary>
    public sealed class WorldRegionData
    {
        public Vector3 ServerCenter;
        public readonly HashSet<Vector2Int> FloraKeys = new HashSet<Vector2Int>();
        public readonly Dictionary<Vector2Int, TerrainFloraTile> Flora = new Dictionary<Vector2Int, TerrainFloraTile>();
        public readonly HashSet<Vector2Int> TerrainKeys = new HashSet<Vector2Int>();
        public readonly HashSet<SwgId> ScenerySwgIds = new HashSet<SwgId>();
        public readonly Dictionary<Vector2Int, WorldTerrainPatch> Terrain = new Dictionary<Vector2Int, WorldTerrainPatch>();
        public readonly Dictionary<SwgId, ModelData> Scenery = new Dictionary<SwgId, ModelData>();
    }
}
