using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Swg.Client.Core.Assets.Formats.Ui;
using Swg.Client.Runtime.Assets.Tre;
using Swg.Client.Runtime.Commands;

namespace Swg.Client.Runtime.Assets.UI
{
    public sealed class CommandVisuals : IDisposable
    {
        readonly Dictionary<string, Sprite> commandIcons = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<string, string> categories = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        readonly List<Sprite> icons = new List<Sprite>();
        bool disposed;
        public Task Loading { get; }

        public CommandVisuals(TreRuntimeAssets assets, CommandCatalog catalog)
        {
            Loading = LoadAsync(assets, catalog);
        }

        async Task LoadAsync(TreRuntimeAssets assets, CommandCatalog catalog)
        {
            await catalog.Loading;
            if (disposed)
                return;
            var read = assets.ReadAsync(loader => (Icons: UiNode.Parse(loader.Text("ui/ui_styles.inc")).FindPath("Styles.Icon"), Paths: loader.Catalog()));
            await read;
            if (disposed)
                return;
            var metadata = read.Result;
            var imageStyles = new Dictionary<string, (UiNode node, string category)>(StringComparer.OrdinalIgnoreCase);
            void Visit(UiNode node, string category)
            {
                string name = node.Get("Name");
                if (string.Equals(name, "command", StringComparison.OrdinalIgnoreCase))
                    category = "combat";
                if (new[]
                {
                    "combat",
                    "social",
                    "mood"
                }.Contains(name.ToLowerInvariant()))
                    category = name.ToLowerInvariant();
                if (node.Get("SourceRect") != "" && !imageStyles.ContainsKey(name))
                    imageStyles.Add(name, (node, category));
                foreach (var alias in node.Attributes)
                {
                    const string prefix = "/styles.icon.";
                    if (!alias.Value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        continue;
                    var target = metadata.Icons.FindPath("Icon." + alias.Value.Substring(prefix.Length));
                    if (target != null && target.Get("SourceRect") != "")
                        imageStyles[alias.Key] = (target, category);
                }

                foreach (var child in node.Children)
                    Visit(child, category);
            }

            Visit(metadata.Icons, "other");
            var spriteCache = new Dictionary<string, Sprite>();
            var assetPaths = metadata.Paths;
            foreach (var command in catalog.Definitions)
            {
                string iconName = command.Name;
                if (iconName == "stand")
                    iconName = "upright";
                else if (iconName == "kneel")
                    iconName = "crouched";
                else if (iconName == "sit")
                    iconName = "sitting";
                bool matched = imageStyles.TryGetValue(iconName, out var image) || imageStyles.TryGetValue("CMD_" + iconName, out image);
                if (!matched)
                    imageStyles.TryGetValue("CMD_uiCombatTarget", out image);
                if (image.node != null && assetPaths.ContainsKey("texture/" + image.node.Get("Source") + ".dds"))
                {
                    if (matched && (image.category == "social" || image.category == "mood"))
                        categories[command.Name] = image.category;
                    string key = image.node.Get("Source") + ":" + image.node.Get("SourceRect");
                    if (!spriteCache.TryGetValue(key, out var sprite))
                    {
                        var coords = image.node.Get("SourceRect").Split(',').Select(int.Parse).ToArray();
                        string path = "texture/" + image.node.Get("Source") + ".dds";
                        var texture = assets.LoadTexture(path);
                        sprite = Sprite.Create(texture, new Rect(Math.Min(coords[0], coords[2]), texture.height - Math.Max(coords[1], coords[3]), Math.Abs(coords[2] - coords[0]), Math.Abs(coords[3] - coords[1])), Vector2.one * .5f, 100);
                        spriteCache.Add(key, sprite);
                        icons.Add(sprite);
                    }

                    commandIcons[command.Name] = sprite;
                }

            }

        }

        public Sprite GetIcon(CommandCatalog.Command command) => command != null && commandIcons.TryGetValue(command.Name, out var icon) ? icon : null;
        public string GetCategory(CommandCatalog.Command command) => categories.TryGetValue(command.Name, out var category) ? category : command.Category;

        public void Dispose()
        {
            disposed = true;
            foreach (var icon in icons)
                UnityEngine.Object.Destroy(icon);
            icons.Clear();
            commandIcons.Clear();
            categories.Clear();
        }
    }
}
