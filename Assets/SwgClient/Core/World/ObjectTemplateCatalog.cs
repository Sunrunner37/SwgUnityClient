using Swg.Client.Core.Assets.Formats.ObjectTemplates;
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
        readonly TreAssetLoader loader;
        readonly Dictionary<string, bool> targetable = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<uint, string> names = new Dictionary<uint, string>();
        public ObjectTemplateCatalog(TreAssetLoader loader)
        {
            this.loader = loader;
            foreach (string path in loader.Catalog().Keys.Where(p => p.StartsWith("object/", StringComparison.Ordinal) && p.EndsWith(".iff", StringComparison.Ordinal)).OrderBy(p => p, StringComparer.Ordinal))
            {
                names[ObjectTemplateNames.TemplateCrc(path)] = path;
                int slash = path.LastIndexOf('/') + 1;
                if (path.Substring(slash).StartsWith("shared_", StringComparison.Ordinal))
                    names[ObjectTemplateNames.TemplateCrc(path.Remove(slash, 7))] = path;
            }
        }

        public bool IsTargetable(string templatePath)
        {
            if (string.IsNullOrEmpty(templatePath))
                return false;
            if (!targetable.TryGetValue(templatePath, out bool value))
            {
                var template = SharedTangibleTemplate.Parse(loader.Read(templatePath));
                value = template.Targetable ?? IsTargetable(template.BaseTemplate);
                targetable.Add(templatePath, value);
            }
            return value;
        }

        readonly Dictionary<string, int> capacities = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        public int ContainerCapacity(string templatePath)
        {
            if (string.IsNullOrEmpty(templatePath))
                return 0;
            if (!capacities.TryGetValue(templatePath, out int capacity))
            {
                var template = SharedTangibleTemplate.Parse(loader.Read(templatePath));
                capacity = template.ContainerCapacity ?? ContainerCapacity(template.BaseTemplate);
                capacities.Add(templatePath, capacity);
            }
            return capacity;
        }

        public string Resolve(uint crc) => names.TryGetValue(crc, out var path) ? path : null;
    }
}
