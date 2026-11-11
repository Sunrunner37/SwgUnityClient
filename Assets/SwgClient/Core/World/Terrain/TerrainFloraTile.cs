using System.Collections.Generic;
using Swg.Client.Core.Assets.Models;

namespace Swg.Client.Core.World.Terrain
{
    public sealed class TerrainFloraTile
    {
        public readonly List<TerrainFloraPlacement> Placements = new List<TerrainFloraPlacement>();
        public readonly Dictionary<string, ModelData> Models = new Dictionary<string, ModelData>();
    }
}
