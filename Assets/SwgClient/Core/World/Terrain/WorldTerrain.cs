using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Assets.Formats.Iff;
using Swg.Client.Core.Assets.Formats.Dds;
using Swg.Client.Core.Assets.Tre;

namespace Swg.Client.Core.World.Terrain
{
    /// <summary>Evaluates native PTAT terrain layers directly from the mounted TRE archives.</summary>
    public sealed class WorldTerrain
    {
        readonly Dictionary<int, NativeTerrainFractal> fractals = new Dictionary<int, NativeTerrainFractal>();
        readonly List<TerrainLayer> layers = new List<TerrainLayer>();
        readonly Dictionary<int, byte[]> shaderColors = new Dictionary<int, byte[]>();
        readonly Dictionary<int, TerrainSurface> surfaces = new Dictionary<int, TerrainSurface>();
        readonly HashSet<int> loadedSurfaces = new HashSet<int>();
        TreAssetLoader assets;
        public float Size { get; private set; }
        public float ChunkSize { get; private set; }
        public bool HasGlobalWater { get; private set; }
        public float GlobalWaterHeight { get; private set; }
        public string Path { get; private set; }

        public static WorldTerrain Load(TreAssetLoader loader, string terrainPath)
        {
            var result = new WorldTerrain
            {
                Path = terrainPath,
                assets = loader
            };
            var root = IffNode.Form(IffNode.Parse(loader.Read(terrainPath)), "PTAT") ?? throw new InvalidDataException("Missing PTAT terrain form");
            var version = Version(root);
            if (version.Kind != "0013" && version.Kind != "0014")
                throw new NotSupportedException("Terrain PTAT version " + version.Kind);
            var header = new ByteReader(version.Chunk("DATA"));
            header.CString();
            result.Size = header.Float();
            result.ChunkSize = header.Float();
            header.U32();
            result.HasGlobalWater = header.U32() != 0;
            result.GlobalWaterHeight = header.Float();
            if (result.Size <= 0 || result.Size > 1000000)
                throw new InvalidDataException("Invalid terrain size");
            var generator = IffNode.Form(version.Children, "TGEN") ?? throw new InvalidDataException("Missing terrain generator");
            foreach (var family in Descendants(generator).Where(n => n.Kind == "MFAM"))
            {
                var r = new ByteReader(family.Chunk("DATA"));
                int id = checked((int)r.U32());
                result.fractals[id] = new NativeTerrainFractal(Version(IffNode.Form(family.Children, "MFRC")).Chunk("DATA"));
            }

            foreach (var family in Descendants(generator).Where(n => n.Tag == "SFAM"))
            {
                var r = new ByteReader(family.Data);
                int id = checked((int)r.U32());
                string name = r.CString();
                r.CString();
                result.shaderColors[id] = new[]
                {
                    r.Byte(),
                    r.Byte(),
                    r.Byte(),
                    (byte)255
                };
                float tileSize = r.Float();
                r.Float();
                int count = checked((int)r.U32());
                if (count < 0 || count > 4096)
                    throw new InvalidDataException("Invalid terrain shader count");
                string shaderPath = null;
                float bestWeight = float.MinValue;
                for (int i = 0; i < count; i++)
                {
                    string shader = r.CString();
                    float weight = r.Float();
                    if (weight > bestWeight)
                    {
                        shaderPath = shader;
                        bestWeight = weight;
                    }
                }

                if (shaderPath != null)
                {
                    shaderPath = TreAssetLoader.Normalize(shaderPath);
                    if (!shaderPath.StartsWith("shader/", StringComparison.OrdinalIgnoreCase))
                        shaderPath = "shader/" + shaderPath;
                    if (!shaderPath.EndsWith(".sht", StringComparison.OrdinalIgnoreCase))
                        shaderPath += ".sht";
                }

                result.surfaces[id] = new TerrainSurface
                {
                    FamilyId = id,
                    Name = name,
                    ShaderPath = shaderPath,
                    TileSize = tileSize
                };
            }

            var layerGroup = IffNode.Form(generator.Children, "LYRS");
            var layerNodes = layerGroup != null ? DescendantsWithoutLayers(layerGroup) : DescendantsWithoutLayers(generator);
            foreach (var layer in layerNodes.Where(n => n.Kind == "LAYR"))
                result.layers.Add(result.ReadLayer(layer));
            if (result.layers.Count == 0)
                throw new InvalidDataException("Terrain contains no layers");
            Debug.LogWarning("Native roads, flora, radial objects and color-ramp affectors are not rendered; shader-family colors are used for ground shading.");
            Debug.LogWarning("Height evaluation follows Core3 terrain processing; slope/shader filters reject height layers as in Core3.");
            return result;
        }

        public float SampleHeight(float x, float z)
        {
            float height = 0;
            int shader = 0;
            foreach (var layer in layers)
                layer.Apply(x, z, 1, ref height, ref shader);
            return height;
        }

        public WorldTerrainPatch BuildPatch(Vector3 center, float radius, float spacing)
        {
            if (radius <= 0 || spacing <= 0)
                throw new ArgumentOutOfRangeException(nameof(radius));
            int cells = checked((int)Math.Ceiling(radius * 2 / spacing));
            if (cells > 512)
                throw new ArgumentOutOfRangeException(nameof(spacing), "Terrain patches are limited to 512 cells per edge");
            float startX = (float)Math.Floor((center.x - radius) / spacing) * spacing;
            float startZ = (float)Math.Floor((center.z - radius) / spacing) * spacing;
            int edge = cells + 1;
            var patch = new WorldTerrainPatch(edge * edge, cells * cells * 6);
            for (int z = 0; z < edge; z++)
                for (int x = 0; x < edge; x++)
                {
                    float worldX = startX + x * spacing;
                    float worldZ = startZ + z * spacing;
                    float height = 0;
                    int shader = 0;
                    foreach (var layer in layers)
                        layer.Apply(worldX, worldZ, 1, ref height, ref shader);
                    int index = z * edge + x;
                    patch.Vertices[index] = new Vector3(worldX, height, worldZ);
                    patch.Uvs[index] = new Vector2(worldX / 8, worldZ / 8);
                    patch.ShaderFamilies[index] = shader;
                    byte[] color;
                    if (!shaderColors.TryGetValue(shader, out color))
                        color = new byte[]
                        {
                            128,
                            128,
                            128,
                            255
                        };
                    Buffer.BlockCopy(color, 0, patch.Colors, index * 4, 4);
                }

            for (int z = 0; z < edge; z++)
                for (int x = 0; x < edge; x++)
                {
                    float dx = patch.Vertices[z * edge + Math.Max(0, x - 1)].y - patch.Vertices[z * edge + Math.Min(cells, x + 1)].y;
                    float dz = patch.Vertices[Math.Max(0, z - 1) * edge + x].y - patch.Vertices[Math.Min(cells, z + 1) * edge + x].y;
                    float ny = spacing * 2;
                    float inverse = 1 / (float)Math.Sqrt(dx * dx + ny * ny + dz * dz);
                    patch.Normals[z * edge + x] = new Vector3(dx * inverse, ny * inverse, dz * inverse);
                }

            int triangle = 0;
            for (int z = 0; z < cells; z++)
                for (int x = 0; x < cells; x++)
                {
                    int a = z * edge + x;
                    patch.Triangles[triangle++] = a;
                    patch.Triangles[triangle++] = a + edge;
                    patch.Triangles[triangle++] = a + 1;
                    patch.Triangles[triangle++] = a + 1;
                    patch.Triangles[triangle++] = a + edge;
                    patch.Triangles[triangle++] = a + edge + 1;
                }

            patch.Surfaces = patch.ShaderFamilies.Distinct().Select(GetSurface).Where(surface => surface != null).ToArray();
            return patch;
        }

        public TerrainSurface GetSurface(int familyId)
        {
            TerrainSurface surface;
            if (!surfaces.TryGetValue(familyId, out surface))
                return null;
            if (loadedSurfaces.Add(familyId) && surface.ShaderPath != null)
            {
                try
                {
                    var shaderNodes = IffNode.Parse(assets.Read(surface.ShaderPath));
                    var bindings = IffNode.Form(shaderNodes, "TXMS");
                    if (bindings != null)
                        foreach (var binding in bindings.Children.Where(node => node.Kind == "TXM "))
                        {
                            var bindingVersion = Version(binding);
                            var tag = bindingVersion.Chunk("DATA");
                            // SWG serializes the texture binding's four-character tag as a little-endian integer.
                            if (tag.Length >= 4 && tag[0] == 'N' && tag[1] == 'I' && tag[2] == 'A' && tag[3] == 'M')
                            {
                                surface.TexturePath = TreAssetLoader.Normalize(new ByteReader(bindingVersion.Chunk("NAME")).CString());
                                break;
                            }
                        }

                    if (surface.TexturePath != null)
                        surface.Diffuse = TextureData.Load(assets, surface.TexturePath);
                    else
                        Debug.LogWarning("Terrain shader has no MAIN texture: " + surface.ShaderPath);
                }
                catch (Exception exception)when (exception is IOException || exception is InvalidOperationException || exception is NotSupportedException)
                {
                    Debug.LogWarning("Terrain shader unavailable: " + surface.ShaderPath + " (" + exception.Message + ")");
                }
            }

            return surface;
        }

        TerrainLayer ReadLayer(IffNode node)
        {
            var version = Version(node);
            var result = new TerrainLayer();
            result.Enabled = Enabled(version);
            var flags = new ByteReader(version.Chunk("ADTA"));
            result.InvertBoundaries = flags.U32() != 0;
            result.InvertFilters = flags.U32() != 0;
            foreach (var child in version.Children.Where(n => n.Tag == "FORM" && n.Kind != "IHDR"))
            {
                if (child.Kind == "LAYR")
                {
                    result.Children.Add(ReadLayer(child));
                    continue;
                }

                var rule = Version(child);
                if (!Enabled(rule))
                    continue;
                byte[] data = rule.Children.FirstOrDefault(n => n.Tag == "DATA")?.Data;
                if (data == null)
                    data = IffNode.Form(rule.Children, "DATA")?.Children.FirstOrDefault(n => n.Tag == "PARM")?.Data;
                switch (child.Kind)
                {
                    case "BCIR":
                    case "BREC":
                    case "BPOL":
                    case "BPLN":
                        result.Boundaries.Add(new TerrainBoundary(child.Kind, data));
                        break;
                    case "AHCN":
                    case "AHFR":
                    case "AHTR":
                    case "ASCN":
                        result.Affectors.Add(ReadAffector(child.Kind, data));
                        break;
                    case "FHGT":
                    case "FFRA":
                    case "FSLP":
                    case "FSHD":
                        result.Filters.Add(ReadFilter(child.Kind, data));
                        break;
                    default:
                        Debug.LogWarning("Terrain rule not evaluated: " + child.Kind);
                        break;
                }
            }

            return result;
        }

        TerrainLayer.Affector ReadAffector(string kind, byte[] data)
        {
            var r = new ByteReader(data);
            if (kind == "ASCN")
            {
                int shader = checked((int)r.U32());
                return (float x, float z, float weight, ref float height, ref int currentShader) =>
                {
                    if (weight >= 0.5f)
                        currentShader = shader;
                };
            }

            if (kind == "AHTR")
            {
                float flat = r.Float();
                float step = r.Float();
                return (float x, float z, float weight, ref float height, ref int shader) =>
                {
                    if (step <= 0)
                        return;
                    float remainder = height % step;
                    if (height == 0)
                        remainder += step;
                    float target = height - remainder;
                    float start = step * flat + target;
                    if (height > start)
                        target += (height - start) / (step * (1 - flat)) * step;
                    height += (target - height) * weight;
                };
            }

            NativeTerrainFractal fractal = null;
            if (kind == "AHFR")
                fractal = GetFractal(checked((int)r.U32()));
            int operation = checked((int)r.U32());
            float value = r.Float();
            return (float x, float z, float weight, ref float height, ref int shader) =>
            {
                float target = fractal == null ? value : value * fractal.Sample(x, z);
                switch (operation)
                {
                    case 1:
                        height += target * weight;
                        break;
                    case 2:
                        height -= target * weight;
                        break;
                    case 3:
                        height += (height * target - height) * weight;
                        break;
                    case 4:
                        if (fractal == null)
                            height = 0;
                        break;
                    default:
                        height += (target - height) * weight;
                        break;
                }
            };
        }

        Func<float, float, float, float> ReadFilter(string kind, byte[] data)
        {
            if (kind == "FSLP" || kind == "FSHD")
                return (x, z, height) => 0;
            var r = new ByteReader(data);
            NativeTerrainFractal fractal = null;
            int featherType;
            float feather;
            float minimum;
            float maximum;
            float multiplier = 1;
            if (kind == "FFRA")
            {
                fractal = GetFractal(checked((int)r.U32()));
                featherType = checked((int)r.U32());
                feather = r.Float();
                minimum = r.Float();
                maximum = r.Float();
                multiplier = r.Float();
            }
            else
            {
                minimum = r.Float();
                maximum = r.Float();
                featherType = checked((int)r.U32());
                feather = r.Float();
            }

            return (x, z, height) =>
            {
                float value = fractal == null ? height : fractal.Sample(x, z) * multiplier;
                if (value <= minimum || value >= maximum)
                    return 0;
                float width = (maximum - minimum) * feather * 0.5f;
                return TerrainBoundary.Feather(width <= 0 ? 1 : Math.Min(1, Math.Min(value - minimum, maximum - value) / width), featherType);
            };
        }

        NativeTerrainFractal GetFractal(int id) => fractals.TryGetValue(id, out var fractal) ? fractal : throw new InvalidDataException("Missing terrain fractal " + id);
        static bool Enabled(IffNode node)
        {
            var header = IffNode.Form(node.Children, "IHDR");
            return header == null || new ByteReader(Version(header).Chunk("DATA")).U32() != 0;
        }

        static IffNode Version(IffNode node) => node.Children.First(n => n.Tag == "FORM");
        static IEnumerable<IffNode> Descendants(IffNode node)
        {
            foreach (var child in node.Children)
            {
                yield return child;
                foreach (var nested in Descendants(child))
                    yield return nested;
            }
        }

        static IEnumerable<IffNode> DescendantsWithoutLayers(IffNode node)
        {
            foreach (var child in node.Children)
            {
                if (child.Kind == "LAYR")
                    yield return child;
                else
                    foreach (var nested in DescendantsWithoutLayers(child))
                        yield return nested;
            }
        }
    }
}
