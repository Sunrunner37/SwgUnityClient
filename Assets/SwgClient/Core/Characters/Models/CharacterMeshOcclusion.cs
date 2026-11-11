using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Assets.Formats.Iff;

namespace Swg.Client.Core.Characters.Models
{
    /// <summary>Uses MGN clothing layers and named occlusion zones to hide covered body/clothing triangles.</summary>
    public static class CharacterMeshOcclusion
    {
        sealed class MeshZones
        {
            public int Layer;
            public string[] Occludes;
            public string[] FullyOccludedBy;
            public string[][] Combinations;

            public MeshZones(IffNode version)
            {
                var info = new ByteReader(version.Chunk("INFO"));
                info.Skip(36);
                int nameCount = info.U16();
                int combinationCount = info.U16();
                int occludesCount = info.U16();
                Layer = unchecked((short)info.U16());
                var zoneNames = version.Children.FirstOrDefault(node => node.Tag == "OZN ");
                var names = zoneNames == null ? Array.Empty<string>() : Encoding.ASCII.GetString(zoneNames.Data).Split(new[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);
                if (names.Length != nameCount)
                    throw new InvalidDataException("Invalid MGN occlusion zone names");
                string[] ReadZones(ByteReader reader, int count)
                {
                    var zones = new string[count];
                    for (int i = 0; i < count; i++)
                        zones[i] = names[reader.U16()];
                    return zones;
                }
                var zto = version.Children.FirstOrDefault(node => node.Tag == "ZTO ");
                Occludes = zto == null ? Array.Empty<string>() : ReadZones(new ByteReader(zto.Data), occludesCount);
                var full = version.Children.FirstOrDefault(node => node.Tag == "FOZC");
                if (full == null)
                    FullyOccludedBy = Array.Empty<string>();
                else
                {
                    var reader = new ByteReader(full.Data);
                    FullyOccludedBy = ReadZones(reader, reader.U16());
                }
                Combinations = new string[combinationCount][];
                if (combinationCount > 0)
                {
                    var reader = new ByteReader(version.Chunk("OZC "));
                    for (int i = 0; i < combinationCount; i++)
                        Combinations[i] = ReadZones(reader, reader.U16());
                }
            }
        }

        public static void Apply(CachedCharacterModelResult model)
        {
            var meshes = model.Sources.Select(source => source.MeshVersion).Distinct().ToDictionary(version => version, version => new MeshZones(version));
            for (int partIndex = model.Parts.Count - 1; partIndex >= 0; partIndex--)
            {
                var source = model.Sources[partIndex];
                var zones = meshes[source.MeshVersion];
                var covered = new HashSet<string>(meshes.Values.Where(other => other.Layer > zones.Layer).SelectMany(other => other.Occludes), StringComparer.OrdinalIgnoreCase);
                bool hidden = zones.FullyOccludedBy.Length > 0 && zones.FullyOccludedBy.All(covered.Contains);
                var part = model.Parts[partIndex];
                var triangles = new List<int>();
                if (!hidden)
                {
                    for (int triangle = 0; triangle < source.TriangleOcclusionCombinations.Length; triangle++)
                    {
                        int combination = source.TriangleOcclusionCombinations[triangle];
                        bool occluded = combination >= 0 && combination < zones.Combinations.Length && zones.Combinations[combination].All(covered.Contains);
                        if (!occluded)
                            for (int vertex = 0; vertex < 3; vertex++)
                                triangles.Add(part.Triangles[triangle * 3 + vertex]);
                    }
                }
                if (triangles.Count == 0)
                {
                    model.Parts.RemoveAt(partIndex);
                    model.Sources.RemoveAt(partIndex);
                }
                else
                    part.Triangles = triangles.ToArray();
            }
        }
    }
}
