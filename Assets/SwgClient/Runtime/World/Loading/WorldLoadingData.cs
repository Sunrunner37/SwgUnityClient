using Swg.Client.Runtime.World.Streaming;
using System.Collections.Generic;
using Swg.Client.Core;
using Swg.Client.Core.Assets.Appearances;

namespace Swg.Client.Runtime.World.Loading
{
    /// <summary>One-time assets handed from the loading screen to the Unity world scene.</summary>
    public sealed class WorldLoadingData
    {
        public WorldRegionData InitialRegion;
        public readonly Dictionary<SwgId, AppearanceData> InitialAppearances = new Dictionary<SwgId, AppearanceData>();
    }
}
