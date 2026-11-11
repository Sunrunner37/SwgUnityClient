using System.Collections.Generic;

namespace Swg.Client.Core.Assets.Appearances
{
    /// <summary>Decoded appearances for one game session; access is serialized by WorldContentLoader.</summary>
    public sealed class AppearanceCache
    {
        readonly Dictionary<string, AppearanceData> appearances = new Dictionary<string, AppearanceData>();
        public int Count => appearances.Count;

        public bool TryGet(string key, out AppearanceData appearance) => appearances.TryGetValue(key, out appearance);
        internal void Add(string key, AppearanceData appearance) => appearances.Add(key, appearance);
    }
}
