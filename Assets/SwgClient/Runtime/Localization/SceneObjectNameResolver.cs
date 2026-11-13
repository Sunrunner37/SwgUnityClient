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

        public string Resolve(SceneObject sceneObject)
        {
            if (!string.IsNullOrEmpty(sceneObject.Name) && sceneObject.Name != sceneObject.TemplatePath)
                return localizer.Localize(sceneObject.Name);
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
