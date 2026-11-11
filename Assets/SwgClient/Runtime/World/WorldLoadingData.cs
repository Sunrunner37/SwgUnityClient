using System.Collections.Generic;
using Swg.Client.Core;
using Swg.Client.Core.Assets.Models;

namespace Swg.Client.Runtime.World
{
    /// <summary>One-time assets handed from the loading screen to the Unity world scene.</summary>
    public sealed class WorldLoadingData
    {
        public WorldRegionData InitialRegion;
        public readonly Dictionary<SwgId, ModelData> InitialModels = new Dictionary<SwgId, ModelData>();
    }
}
