using System;
using System.Collections.Generic;
using System.Linq;
using Swg.Client.Core.Assets.Formats.Trn;

namespace Swg.Client.Core.World.Terrain
{
    public sealed class TerrainFloraCatalog
    {
        sealed class Family
        {
            public float Density;
            public readonly List<TerrainFloraItem> Items = new List<TerrainFloraItem>();
        }

        readonly Dictionary<int, Family> flora = new Dictionary<int, Family>();
        readonly Dictionary<int, Family> radial = new Dictionary<int, Family>();
        internal TerrainFloraCatalog(IEnumerable<TrnFloraFamily> definitions)
        {
            foreach (var definition in definitions)
            {
                var family = new Family
                {
                    Density = definition.Density
                };
                foreach (var child in definition.Children)
                {
                    string prefix = definition.Radial ? "shader/" : "appearance/";
                    string path = child.Name.Replace('\\', '/').ToLowerInvariant();
                    if (!path.StartsWith(prefix, StringComparison.Ordinal))
                        path = prefix + path;
                    if (definition.Radial && !path.EndsWith(".sht", StringComparison.Ordinal))
                        path += ".sht";
                    family.Items.Add(new TerrainFloraItem { Path = path, Weight = child.Weight, Radial = definition.Radial, Floats = definition.Floats, Distance = child.Distance, Width = (child.MinimumWidth + child.MaximumWidth) * .5f, Height = (child.MinimumHeight + child.MaximumHeight) * .5f, SwayDisplacement = child.Sways ? child.SwayDisplacement : 0, SwayPeriod = child.SwayPeriod, AlignToTerrain = child.AlignToTerrain, Crossed = child.Crossed, MinimumScale = child.Scale ? child.MinimumScale : 1, MaximumScale = child.Scale ? child.MaximumScale : 1 });
                }

                (definition.Radial ? radial : flora).Add(definition.Id, family);
            }
        }

        internal float Density(int familyId, bool dynamic) => (dynamic ? radial : flora).TryGetValue(familyId, out var family) ? family.Density : 0;
        public TerrainFloraItem Select(TerrainFloraSelection selection, bool dynamic)
        {
            if (!(dynamic ? radial : flora).TryGetValue(selection.FamilyId, out var family) || family.Items.Count == 0)
                return null;
            float choice = selection.ChildChoice * family.Items.Sum(item => item.Weight);
            foreach (var item in family.Items)
            {
                choice -= item.Weight;
                if (choice <= 0)
                    return item;
            }

            return family.Items[family.Items.Count - 1];
        }
    }
}
