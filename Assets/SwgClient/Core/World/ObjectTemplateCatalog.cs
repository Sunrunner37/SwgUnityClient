using Swg.Client.Core.Assets;
using System;
using System.Collections.Generic;
using System.Linq;
using Swg.Client.Core.Assets.Tre;
using Swg.Client.Core.Characters.Appearance;

namespace Swg.Client.Core.World
{
    public sealed class ObjectTemplateCatalog
    {
        readonly Dictionary<uint, string> names = new Dictionary<uint, string>();
        public ObjectTemplateCatalog(TreAssetLoader loader)
        {
            foreach (string path in loader.Catalog().Keys.Where(p => p.StartsWith("object/", StringComparison.Ordinal) && p.EndsWith(".iff", StringComparison.Ordinal)).OrderBy(p => p, StringComparer.Ordinal))
            {
                names[ObjectTemplateNames.TemplateCrc(path)] = path;
                int slash = path.LastIndexOf('/') + 1;
                if (path.Substring(slash).StartsWith("shared_", StringComparison.Ordinal))
                    names[ObjectTemplateNames.TemplateCrc(path.Remove(slash, 7))] = path;
            }
        }

        public string Resolve(uint crc) => names.TryGetValue(crc, out var path) ? path : null;
    }
}
