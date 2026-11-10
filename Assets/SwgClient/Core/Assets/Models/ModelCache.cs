using System.Collections.Generic;

namespace Swg.Client.Core.Assets.Models
{
    /// <summary>Decoded models for one game session; access is serialized by WorldContentLoader.</summary>
    public sealed class ModelCache
    {
        readonly Dictionary<string, ModelData> models = new Dictionary<string, ModelData>();
        public int Count => models.Count;

        public bool TryGet(string key, out ModelData model) => models.TryGetValue(key, out model);
        internal void Add(string key, ModelData model) => models.Add(key, model);
    }
}
