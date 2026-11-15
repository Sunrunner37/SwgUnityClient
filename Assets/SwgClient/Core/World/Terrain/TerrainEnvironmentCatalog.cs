using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Swg.Client.Core.Assets.Formats.Trn;
using Swg.Client.Core.Assets.Formats.DataTable;
using Swg.Client.Core.Assets.Formats.Tga;
using Swg.Client.Core.Assets.Formats.Dds;
using Swg.Client.Core.Assets.Formats.Iff;
using Swg.Client.Core.Assets.Formats.Sht;
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
            [DataColumn("Gradient Sky Texture (256x256 dds)")]
            public string SkyGradient;
            [DataColumn("Cloud Layer Bottom Shader (sht)")]
            public string BottomShader;
            [DataColumn("Cloud Layer Bottom Shader Size", Optional = true)]
            public float BottomSize;
            [DataColumn("Cloud Layer Bottom Speed", Optional = true)]
            public float BottomSpeed;
            [DataColumn("Cloud Layer Top Shader (sht)")]
            public string TopShader;
            [DataColumn("Cloud Layer Top Shader Size", Optional = true)]
            public float TopSize;
            [DataColumn("Cloud Layer Top Speed", Optional = true)]
            public float TopSpeed;
            [DataColumn("Lighting Color Ramps (256x8 tga)")]
            public string ColorRamp;
            [DataColumn("Fog Enabled")]
            public int Fog;
            [DataColumn("Minimum Fog Density", Optional = true)]
            public float? MinimumFog;
            [DataColumn("Maximum Fog Density", Optional = true)]
            public float? MaximumFog;
            [DataColumn("Fog Density", Optional = true)]
            public float FogDensity;
            [DataColumn("Shadows Enabled", Optional = true)]
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
            var textures = new Dictionary<string, TextureData>(StringComparer.OrdinalIgnoreCase);
            TextureData Texture(string name)
            {
                if (string.IsNullOrEmpty(name)) return null;
                name = name.Replace('\\', '/');
                if (!name.StartsWith("texture/", StringComparison.OrdinalIgnoreCase)) name = "texture/" + name;
                if (!textures.TryGetValue(name, out var texture))
                    textures.Add(name, texture = TextureData.Load(assets, name));
                return texture;
            }
            TerrainEnvironmentData.CloudLayer Clouds(string shader, float size, float speed)
            {
                if (string.IsNullOrEmpty(shader)) return null;
                shader = shader.Replace('\\', '/');
                if (!shader.StartsWith("shader/", StringComparison.OrdinalIgnoreCase)) shader = "shader/" + shader;
                var nodes = IffNode.Parse(assets.Read(shader));
                if (!ShtFile.TryGetMainTexture(nodes, out var diffuse)) return null;
                ShtFile.TryGetTexture(nodes, "NRML", out var normal);
                return new TerrainEnvironmentData.CloudLayer { Diffuse = Texture(diffuse), Normal = Texture(normal), Size = size, Speed = speed };
            }
            foreach (var row in rows.Where(row => row.Weather == 0 && !string.IsNullOrEmpty(row.ColorRamp)))
            {
                lighting[row.Name] = new TerrainEnvironmentData
                {
                    Name = row.Name,
                    SkyGradient = Texture(row.SkyGradient),
                    BottomClouds = Clouds(row.BottomShader, row.BottomSize, row.BottomSpeed),
                    TopClouds = Clouds(row.TopShader, row.TopSize, row.TopSpeed),
                    ColorRamp = TgaFile.Parse(assets.Read(row.ColorRamp)),
                    FogEnabled = row.Fog != 0,
                    MinimumFogDensity = row.MinimumFog ?? row.FogDensity,
                    MaximumFogDensity = row.MaximumFog ?? row.FogDensity,
                    ShadowsEnabled = row.Shadows != 0
                };
            }
        }
    }
}
