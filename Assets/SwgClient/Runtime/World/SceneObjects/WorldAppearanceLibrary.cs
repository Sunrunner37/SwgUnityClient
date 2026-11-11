using Swg.Client.Runtime.Assets.Appearances;
using Swg.Client.Runtime.World;
using System;
using System.Collections.Generic;
using System.Linq;
using Swg.Client.Core.Assets.Appearances;
using Swg.Client.Runtime.Assets.Shaders;
using UnityEngine;

namespace Swg.Client.Runtime.World.SceneObjects
{
    /// <summary>Caches shared appearance resources for the loaded world.</summary>
    public sealed class WorldAppearanceLibrary : IDisposable
    {
        readonly SwgMaterialFactory materials;
        readonly WorldRenderingContext context;
        readonly Dictionary<string, AppearanceResources> appearances = new Dictionary<string, AppearanceResources>(StringComparer.Ordinal);
        public WorldAppearanceLibrary(SwgMaterialFactory materials, WorldRenderingContext context)
        {
            this.materials = materials;
            this.context = context;
        }

        public GameObject Create(AppearanceData appearance, Transform parent)
        {
            if (!appearances.TryGetValue(appearance.Key, out var resources))
                appearances.Add(appearance.Key, resources = new AppearanceResources(appearance, materials, context.WorldShader, context.CharacterShader, context.ParticleShader));
            return resources.Create(parent);
        }

        public void PruneAppearances(IEnumerable<string> residentKeys)
        {
            var keep = new HashSet<string>(residentKeys, StringComparer.Ordinal);
            foreach (var key in appearances.Keys.Where(key => !keep.Contains(key)).ToArray())
            {
                appearances[key].Dispose();
                appearances.Remove(key);
            }
        }

        public void Dispose()
        {
            foreach (var appearance in appearances.Values)
                appearance.Dispose();
            appearances.Clear();
        }
    }
}
