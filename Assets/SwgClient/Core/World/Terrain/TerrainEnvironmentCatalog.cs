using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Swg.Client.Core.Assets.Formats.Trn;
using Swg.Client.Core.Assets.Formats.DataTable;
using Swg.Client.Core.Assets.Formats.Tga;
using Swg.Client.Core.Assets.Tre;

namespace Swg.Client.Core.World.Terrain
{
    public sealed class TerrainEnvironmentCatalog
    {
        public sealed class Row
        {
            [DataColumn("Environment Family Name")]
            public string Name;
            [DataColumn("Weather Index")]
            public int Weather;
            [DataColumn("Lighting Color Ramps (256x8 tga)")]
            public string ColorRamp;
            [DataColumn("Fog Enabled")]
            public int Fog;
            [DataColumn("Minimum Fog Density")]
            public float MinimumFog;
            [DataColumn("Maximum Fog Density")]
            public float MaximumFog;
            [DataColumn("Shadows Enabled")]
            public int Shadows;
        }

        readonly Dictionary<int, (string Name, float Clamp)> families = new Dictionary<int, (string, float)>();
        readonly Dictionary<string, TerrainEnvironmentData> lighting = new Dictionary<string, TerrainEnvironmentData>(StringComparer.OrdinalIgnoreCase);
        public float FeatherClamp(int id) => families.TryGetValue(id, out var family) ? family.Clamp : 1;
        public TerrainEnvironmentData Get(int id)
        {
            string name = families.TryGetValue(id, out var family) ? family.Name : "global";
            return lighting.TryGetValue(name, out var data) ? data : lighting.TryGetValue("global", out data) ? data : null;
        }

        internal TerrainEnvironmentCatalog(TreAssetLoader assets, IEnumerable<TrnEnvironmentFamily> definitions, string terrainPath)
        {
            foreach (var definition in definitions)
            {
                families.Add(definition.Id, (definition.Name, definition.FeatherClamp));
            }

            string path = "datatables/environment/" + Path.GetFileNameWithoutExtension(terrainPath) + ".iff";
            List<Row> rows = DataTableReader.Read<Row>(assets.Read(path));
            foreach (var row in rows.Where(row => row.Weather == 0 && !string.IsNullOrEmpty(row.ColorRamp)))
            {
                lighting[row.Name] = new TerrainEnvironmentData
                {
                    Name = row.Name,
                    ColorRamp = TgaFile.Parse(assets.Read(row.ColorRamp)),
                    FogEnabled = row.Fog != 0,
                    MinimumFogDensity = row.MinimumFog,
                    MaximumFogDensity = row.MaximumFog,
                    ShadowsEnabled = row.Shadows != 0
                };
            }
        }
    }
}
