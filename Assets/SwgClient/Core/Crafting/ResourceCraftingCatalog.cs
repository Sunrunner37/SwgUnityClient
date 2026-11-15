using System;
using System.Collections.Generic;
using System.Linq;
using Swg.Client.Core.Assets.Formats.DataTable;
using Swg.Client.Core.Assets.Tre;
namespace Swg.Client.Core.Crafting
{
    /// <summary>Resource-tree ancestry determines ingredient eligibility; grades remain server-owned.</summary>
    public sealed class ResourceCraftingCatalog
    {
        readonly Dictionary<string, string[]> classes;
        ResourceCraftingCatalog(Dictionary<string, string[]> classes) => this.classes = classes;
        public static ResourceCraftingCatalog Load(TreAssetLoader loader)
        {
            var table = DataTableFile.Parse(loader.Read("datatables/resource/resource_tree.iff"));
            var paths = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            var ancestry = new string[8];
            foreach (var row in table.Rows)
            {
                for (int depth = 0; depth < ancestry.Length; depth++)
                {
                    string name = row.Get<string>("CLASS " + (depth + 1));
                    if (name.Length == 0) continue;
                    ancestry[depth] = name;
                    Array.Clear(ancestry, depth + 1, ancestry.Length - depth - 1);
                    paths[row.Get<string>("ENUM")] = ancestry.Take(depth + 1).ToArray();
                    break;
                }
            }
            return new ResourceCraftingCatalog(paths);
        }
        public string Name(string resource) => classes.TryGetValue(resource, out var path) ? path.Last() : resource.Replace('_', ' ');
        public bool Matches(string resource, string required)
        {
            if (string.IsNullOrEmpty(resource)) return false;
            if (string.Equals(resource, required, StringComparison.OrdinalIgnoreCase)) return true;
            return classes.TryGetValue(resource, out var actual) && classes.TryGetValue(required, out var parent)
                && parent.Length <= actual.Length && parent.SequenceEqual(actual.Take(parent.Length));
        }
    }
}
