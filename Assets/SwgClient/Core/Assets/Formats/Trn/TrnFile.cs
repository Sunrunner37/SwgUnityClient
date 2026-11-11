using System;
using Swg.Client.Core.World.Terrain;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Assets.Formats.Iff;

namespace Swg.Client.Core.Assets.Formats.Trn
{
    /// <summary>
    /// TRN terrain files describe procedural terrain using an IFF PTAT form: native families,
    /// nested layers, boundaries, filters and affectors. This reader constructs terrain boundaries and filters directly from their native parameters.
    /// </summary>
    /// <seealso href="https://github.com/swg-source/client-tools/tree/master/src/engine/shared/library/sharedTerrain/src/shared/generator">Native terrain generator source</seealso>
    public sealed class TrnFile
    {
        public string Version;
        public string FileName;
        public float Size;
        public float ChunkSize;
        public uint TilesPerChunk;
        public bool HasGlobalWater;
        public float GlobalWaterHeight;
        public readonly TerrainFloraDistribution[] FloraDistributions = new TerrainFloraDistribution[4];
        public readonly Dictionary<int, NativeTerrainFractal> Fractals = new Dictionary<int, NativeTerrainFractal>();
        public readonly List<TrnShaderFamily> Shaders = new List<TrnShaderFamily>();
        public readonly List<TrnFloraFamily> Flora = new List<TrnFloraFamily>();
        public readonly List<TrnEnvironmentFamily> Environments = new List<TrnEnvironmentFamily>();
        public readonly List<TrnLayer> Layers = new List<TrnLayer>();
        public static TrnFile Parse(byte[] bytes)
        {
            var root = IffNode.Form(IffNode.Parse(bytes), "PTAT") ?? throw new InvalidDataException("Missing PTAT terrain form");
            var version = ReadVersion(root);
            if (version.Kind != "0013" && version.Kind != "0014")
                throw new NotSupportedException("Terrain PTAT version " + version.Kind);
            var header = new ByteReader(version.Chunk("DATA"));
            var result = new TrnFile
            {
                Version = version.Kind,
                FileName = header.CString(),
                Size = header.Float(),
                ChunkSize = header.Float(),
                TilesPerChunk = header.U32(),
                HasGlobalWater = header.U32() != 0,
                GlobalWaterHeight = header.Float()
            };
            header.Float(); // Global water shader size.
            header.CString(); // Global water shader.
            header.Float(); // Native day/night cycle duration.
            if (version.Kind == "0013")
            {
                header.CString();
                header.CString();
                header.Float();
                for (int i = 0; i < 4; i++)
                {
                    header.CString();
                    header.Float();
                }

                header.U32();
                header.CString();
            }

            for (int i = 0; i < result.FloraDistributions.Length; i++)
                result.FloraDistributions[i] = new TerrainFloraDistribution
                {
                    MinimumDistance = header.Float(),
                    MaximumDistance = header.Float(),
                    Spacing = header.Float(),
                    Border = header.Float(),
                    Seed = header.U32()
                };
            if (result.Size <= 0 || result.Size > 1000000)
                throw new InvalidDataException("Invalid terrain size");
            var generator = IffNode.Form(version.Children, "TGEN") ?? throw new InvalidDataException("Missing terrain generator");
            foreach (var family in Descendants(generator).Where(node => node.Kind == "MFAM"))
            {
                int id = checked((int)new ByteReader(family.Chunk("DATA")).U32());
                result.Fractals[id] = new NativeTerrainFractal(TrnFractal.Parse(ReadVersion(IffNode.Form(family.Children, "MFRC")).Chunk("DATA")));
            }

            foreach (var family in Descendants(generator).Where(node => node.Tag == "SFAM"))
            {
                var reader = new ByteReader(family.Data);
                var definition = new TrnShaderFamily
                {
                    Id = checked((int)reader.U32()),
                    Name = reader.CString(),
                    SurfaceProperties = reader.CString(),
                    EditorColor = ReadColor(reader),
                    TileSize = reader.Float(),
                    FeatherClamp = reader.Float()
                };
                int count = reader.Count();
                for (int i = 0; i < count; i++)
                    definition.Children.Add(new TrnShaderChild { Path = reader.CString(), Weight = reader.Float() });
                result.Shaders.Add(definition);
            }

            result.ReadFlora(IffNode.Form(generator.Children, "FGRP"), false);
            result.ReadFlora(IffNode.Form(generator.Children, "RGRP"), true);
            var environments = IffNode.Form(generator.Children, "EGRP");
            if (environments != null)
                foreach (var node in ReadVersion(environments).Children.Where(child => child.Kind == "EFAM"))
                {
                    var reader = new ByteReader(node.Chunk("DATA"));
                    result.Environments.Add(new TrnEnvironmentFamily { Id = (int)reader.U32(), Name = reader.CString(), EditorColor = ReadColor(reader), FeatherClamp = reader.Float() });
                }

            var layerGroup = IffNode.Form(generator.Children, "LYRS") ?? generator;
            var filters = new TrnFilterReader(result.Fractals);
            foreach (var layer in LayerNodes(layerGroup))
                result.Layers.Add(ReadLayer(layer, filters));
            if (result.Layers.Count == 0)
                throw new InvalidDataException("Terrain contains no layers");
            return result;
        }

        void ReadFlora(IffNode group, bool dynamic)
        {
            if (group == null)
                return;
            var version = ReadVersion(group);
            if ((!dynamic && version.Kind != "0008") || (dynamic && version.Kind != "0003" && version.Kind != "0004"))
                throw new NotSupportedException("Terrain flora group " + group.Kind + "/" + version.Kind);
            foreach (var node in version.Children.Where(node => node.Tag == (dynamic ? "RFAM" : "FFAM")))
            {
                var reader = new ByteReader(node.Data);
                var family = new TrnFloraFamily
                {
                    Id = (int)reader.U32(),
                    Name = reader.CString(),
                    EditorColor = ReadColor(reader),
                    Density = reader.Float(),
                    Radial = dynamic
                };
                family.Floats = !dynamic && reader.U32() != 0;
                int count = reader.Count();
                for (int i = 0; i < count; i++)
                {
                    var child = new TrnFloraChild
                    {
                        Name = reader.CString(),
                        Weight = reader.Float()
                    };
                    if (dynamic)
                    {
                        child.Distance = reader.Float();
                        child.MinimumWidth = reader.Float();
                        child.MaximumWidth = version.Kind == "0004" ? reader.Float() : child.MinimumWidth;
                        child.MinimumHeight = reader.Float();
                        child.MaximumHeight = version.Kind == "0004" ? reader.Float() : child.MinimumHeight;
                        child.MaintainAspectRatio = version.Kind == "0004" && reader.U32() != 0;
                    }

                    child.Sways = reader.U32() != 0;
                    child.SwayDisplacement = reader.Float();
                    child.SwayPeriod = reader.Float();
                    child.AlignToTerrain = reader.U32() != 0;
                    if (dynamic)
                        child.Crossed = reader.U32() != 0;
                    else
                    {
                        child.Scale = reader.U32() != 0;
                        child.MinimumScale = reader.Float();
                        child.MaximumScale = reader.Float();
                    }

                    family.Children.Add(child);
                }

                Flora.Add(family);
            }
        }

        static TrnLayer ReadLayer(IffNode node, TrnFilterReader filters)
        {
            var version = ReadVersion(node);
            var flags = new ByteReader(version.Chunk("ADTA"));
            var layer = new TrnLayer
            {
                Enabled = Enabled(version),
                InvertBoundaries = flags.U32() != 0,
                InvertFilters = flags.U32() != 0
            };
            foreach (var child in version.Children.Where(child => child.Tag == "FORM" && child.Kind != "IHDR"))
            {
                if (child.Kind == "LAYR")
                {
                    layer.Children.Add(ReadLayer(child, filters));
                    continue;
                }

                var rule = ReadVersion(child);
                if (!Enabled(rule))
                    continue;
                var data = rule.Children.FirstOrDefault(chunk => chunk.Tag == "DATA")?.Data ?? IffNode.Form(rule.Children, "DATA")?.Children.FirstOrDefault(chunk => chunk.Tag == "PARM")?.Data;
                switch (child.Kind)
                {
                    case "BCIR":
                    case "BREC":
                    case "BPOL":
                    case "BPLN":
                        layer.Boundaries.Add(TrnBoundaryReader.Read(child.Kind, data));
                        break;
                    case "AHCN":
                    case "AHFR":
                    case "AHTR":
                    case "ASCN":
                    case "ASRP":
                    case "AFDN":
                    case "AFDF":
                    case "AFSC":
                    case "AFSN":
                    case "AENV":
                    case "AROA":
                    case "ACCN":
                    case "ACRH":
                    case "AEXC":
                    case "ACRF":
                        layer.Affectors.Add(TrnRule.Parse(child.Kind, rule, data));
                        break;
                    case "FHGT":
                    case "FFRA":
                    case "FSLP":
                    case "FSHD":
                        layer.Filters.Add(filters.Read(child.Kind, data));
                        break;
                    default:
                        layer.UnsupportedRules.Add(child.Kind);
                        break;
                }
            }

            return layer;
        }

        static Color32 ReadColor(ByteReader reader)
        {
            var bytes = reader.Take(3);
            return new Color32(bytes[0], bytes[1], bytes[2], 255);
        }

        static bool Enabled(IffNode node)
        {
            var header = IffNode.Form(node.Children, "IHDR");
            return header == null || new ByteReader(ReadVersion(header).Chunk("DATA")).U32() != 0;
        }

        static IffNode ReadVersion(IffNode node) => node.Children.First(child => child.Tag == "FORM");
        static IEnumerable<IffNode> Descendants(IffNode node)
        {
            foreach (var child in node.Children)
            {
                yield return child;
                foreach (var nested in Descendants(child))
                    yield return nested;
            }
        }

        static IEnumerable<IffNode> LayerNodes(IffNode node)
        {
            foreach (var child in node.Children)
                if (child.Kind == "LAYR")
                    yield return child;
                else
                    foreach (var nested in LayerNodes(child))
                        yield return nested;
        }
    }
}
