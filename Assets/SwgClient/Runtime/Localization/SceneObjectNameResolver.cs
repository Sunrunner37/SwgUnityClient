using System.Linq;
using Swg.Client.Core.Assets.Formats.DataTable;
using System;
using System.Collections.Generic;
using System.IO;
using Swg.Client.Core.Assets;
using Swg.Client.Core.World.SceneObjects;
using Swg.Client.Runtime.Assets.Tre;

namespace Swg.Client.Runtime.Localization
{
    /// <summary>Resolves live custom names or cached, localized template names for any object display.</summary>
    public sealed class SceneObjectNameResolver
    {
        readonly TreRuntimeAssets assets;
        readonly StringTableLocalizer localizer;
        readonly Dictionary<string, string> templateNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public SceneObjectNameResolver(TreRuntimeAssets assets, StringTableLocalizer localizer)
        {
            this.assets = assets;
            this.localizer = localizer;
        }

        readonly Dictionary<string, string> descriptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public string Description(SceneObject sceneObject)
        {
            string path = sceneObject.TemplatePath;
            if (string.IsNullOrEmpty(path))
                return "";
            if (!descriptions.TryGetValue(path, out var description))
            {
                description = localizer.Localize(ObjectTemplateNames.Description(assets.Loader, path));
                descriptions.Add(path, description);
            }
            return description;
        }

        Dictionary<string, string> resourceNames;
        string ResourceName(string type)
        {
            if (resourceNames == null)
            {
                resourceNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var hierarchy = new string[8];
                var table = DataTableFile.Parse(assets.Loader.Read("datatables/resource/resource_tree.iff"));
                foreach (var row in table.Rows)
                {
                    int level = -1;
                    for (int i = 0; i < hierarchy.Length; i++)
                    {
                        string label = row.Get<string>("CLASS " + (i + 1));
                        if (string.IsNullOrEmpty(label))
                            continue;
                        hierarchy[i] = label;
                        level = i;
                        for (int j = i + 1; j < hierarchy.Length; j++)
                            hierarchy[j] = null;
                    }
                    // The native inventory labels resources by their parent class, rather than spawn name or subtype.
                    if (level >= 0)
                        resourceNames[row.Get<string>("ENUM")] = hierarchy.Take(level).LastOrDefault(label => !string.IsNullOrEmpty(label)) ?? hierarchy[level];
                }
            }
            return resourceNames.TryGetValue(type, out var name) ? name : type.Replace('_', ' ');
        }

        public string Resolve(SceneObject sceneObject)
        {
            if (!string.IsNullOrEmpty(sceneObject.Name) && sceneObject.Name != sceneObject.TemplatePath)
                return localizer.Localize(sceneObject.Name);
            if (sceneObject is TangibleSceneObject tangible && !string.IsNullOrEmpty(tangible.ResourceType))
                return ResourceName(tangible.ResourceType);
            string path = sceneObject.TemplatePath;
            if (string.IsNullOrEmpty(path))
                return "Object";
            if (!templateNames.TryGetValue(path, out var name))
            {
                name = localizer.Localize(ObjectTemplateNames.DisplayName(assets.Loader, path));
                if (string.IsNullOrEmpty(name))
                    name = Path.GetFileNameWithoutExtension(path).Replace("shared_", "").Replace('_', ' ');
                templateNames.Add(path, name);
            }
            return name;
        }
    }
}
