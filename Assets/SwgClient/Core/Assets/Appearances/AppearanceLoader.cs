using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Assets.Formats.Iff;
using Swg.Client.Core.Assets.Particles;
using Swg.Client.Core.Assets.Tre;
using Swg.Client.Core.Geometry;
using UnityEngine;

namespace Swg.Client.Core.Assets.Appearances
{
    /// <summary>Traverses original templates and compound appearances, collecting mesh geometry and particle effects directly from TRE members.</summary>
    public static class AppearanceLoader
    {
        public const int LowestDetail = int.MaxValue;
        public static List<MeshPart> Load(TreAssetLoader loader, string path) => LoadDetailed(loader, path).Parts;
        public static AppearanceData LoadDetailed(TreAssetLoader loader, string path, int detailLevel = 0, IReadOnlyDictionary<string, short> customization = null)
        {
            var result = new AppearanceData
            {
                Key = TreAssetLoader.Normalize(path) + (detailLevel == 0 ? "" : detailLevel == LowestDetail ? "#lowest" : "#lod" + detailLevel)
            };
            LoadAppearance(loader, TreAssetLoader.Normalize(path), Matrix4x4.identity, result, new HashSet<string>(StringComparer.OrdinalIgnoreCase), 0, detailLevel, customization);
            result.RecalculateBounds();
            return result;
        }

        static void LoadAppearance(TreAssetLoader loader, string path, Matrix4x4 transform, AppearanceData result, HashSet<string> stack, int depth, int detailLevel, IReadOnlyDictionary<string, short> customization)
        {
            if (depth > 24 || !stack.Add(path))
                throw new InvalidDataException("Appearance reference cycle/depth limit: " + path);
            try
            {
                var bytes = loader.Read(path);
                var roots = IffNode.Parse(bytes);
                var root = roots.SingleOrDefault(n => n.Tag == "FORM") ?? throw new InvalidDataException("Expected appearance form: " + path);
                if (path.StartsWith("object/", StringComparison.Ordinal))
                {
                    var appearances = References(bytes, "appearance/", new[] { ".apt", ".lod", ".msh", ".pob", ".cmp", ".prt", ".sat" });
                    if (appearances.Count > 0)
                    {
                        LoadAppearance(loader, appearances[0], transform, result, stack, depth + 1, detailLevel, customization);
                        return;
                    }

                    if (path.StartsWith("object/cell/", StringComparison.Ordinal) || path.StartsWith("object/intangible/", StringComparison.Ordinal))
                        return;
                    foreach (var inherited in References(bytes, "object/", new[] { ".iff" }).Where(p => p != path))
                    {
                        int count = result.Parts.Count + result.Particles.Count;
                        LoadAppearance(loader, inherited, transform, result, stack, depth + 1, detailLevel, customization);
                        if (result.Parts.Count + result.Particles.Count != count)
                            return;
                    }

                    return;
                }

                if (root.Kind == "APT ")
                {
                    var version = root.Children.First(n => n.Tag == "FORM");
                    LoadAppearance(loader, AppearancePath(new ByteReader(version.Chunk("NAME")).CString()), transform, result, stack, depth + 1, detailLevel, customization);
                }
                else if (root.Kind == "DTLA")
                {
                    var version = root.Children.First(n => n.Tag == "FORM");
                    var data = version.Children.Single(n => n.Kind == "DATA");
                    var children = data.Children.Where(n => n.Tag == "CHLD").Reverse().ToArray();
                    var child = children.Length == 0 ? throw new InvalidDataException("Empty detail appearance") : children[Math.Min(detailLevel, children.Length - 1)];
                    var reader = new ByteReader(child.Data);
                    reader.U32();
                    LoadAppearance(loader, AppearancePath(reader.CString()), transform, result, stack, depth + 1, detailLevel, customization);
                }
                else if (root.Kind == "MESH")
                    MeshAppearanceReader.Load(loader, path, root, transform, result, customization);
                else if (root.Kind == "SMAT")
                {
                    var skeletal = SkeletalAppearanceLoader.LoadDetailed(loader, path);
                    result.Particles.AddRange(skeletal.Particles.Select(effect => effect.WithTransform(transform)));
                    foreach (var part in skeletal.Parts)
                    {
                        if (transform != Matrix4x4.identity)
                        {
                            // Compound appearances flatten their child transform into the neutral mesh.
                            part.Vertices = part.Vertices.Select(transform.MultiplyPoint3x4).ToArray();
                            var normalTransform = transform.inverse.transpose;
                            part.Normals = part.Normals.Select(normal => normalTransform.MultiplyVector(normal).normalized).ToArray();
                            part.Skinning = null;
                        }
                        result.Parts.Add(part);
                    }
                }
                else if (root.Kind == "PEFT")
                    result.Particles.Add(new ParticleAppearance(loader, path, transform, root, child =>
                    {
                        var appearance = new AppearanceData
                        {
                            Key = AppearancePath(child)
                        };
                        LoadAppearance(loader, AppearancePath(child), Matrix4x4.identity, appearance, stack, depth + 1, detailLevel, customization);
                        foreach (var warning in appearance.Warnings)
                            Warn(result, warning);
                        appearance.RecalculateBounds();
                        return appearance;
                    }));
                else if (root.Kind == "PRTO")
                {
                    var version = root.Children.First(n => n.Tag == "FORM");
                    var cells = version.Children.Single(n => n.Kind == "CELS");
                    foreach (var cell in cells.Children.Where(n => n.Kind == "CELL"))
                    {
                        var cellVersion = cell.Children.First(n => n.Tag == "FORM");
                        if (cellVersion.Kind != "0004" && cellVersion.Kind != "0005")
                        {
                            Warn(result, "Unsupported portal cell version " + cellVersion.Kind);
                            continue;
                        }

                        var reader = new ByteReader(cellVersion.Chunk("DATA"));
                        reader.U32();
                        reader.Byte();
                        reader.CString();
                        string appearance = reader.CString();
                        if (!string.IsNullOrEmpty(appearance))
                            LoadChild(loader, AppearancePath(appearance), transform, result, stack, depth + 1, detailLevel, customization);
                    }
                }
                else if (root.Kind == "CMPA")
                {
                    var version = root.Children.First(n => n.Tag == "FORM");
                    foreach (var part in version.Children.Where(n => n.Tag == "PART"))
                    {
                        var reader = new ByteReader(part.Data);
                        string appearance = AppearancePath(reader.CString());
                        // CMPA stores three affine rows; Unity indexes matrices by row and column.
                        var local = Matrix4x4.identity;
                        for (int row = 0; row < 3; row++)
                            for (int column = 0; column < 4; column++)
                                local[row, column] = reader.Float();
                        LoadChild(loader, appearance, transform * local, result, stack, depth + 1, detailLevel, customization);
                    }
                }
                else
                    Warn(result, "Unsupported render appearance " + root.Kind + ": " + path);
            }
            finally
            {
                stack.Remove(path);
            }
        }

        static void LoadChild(TreAssetLoader loader, string path, Matrix4x4 matrix, AppearanceData result, HashSet<string> stack, int depth, int detailLevel, IReadOnlyDictionary<string, short> customization)
        {
            try
            {
                LoadAppearance(loader, path, matrix, result, stack, depth, detailLevel, customization);
            }
            catch (Exception e)when (e is IOException || e is InvalidOperationException || e is ArgumentException)
            {
                Warn(result, "Appearance could not be loaded " + path + ": " + e.Message);
            }
        }

        static string AppearancePath(string path)
        {
            path = TreAssetLoader.Normalize(path);
            return path.StartsWith("appearance/", StringComparison.Ordinal) ? path : "appearance/" + path;
        }

        static List<string> References(byte[] bytes, string prefix, string[] suffixes)
        {
            string pattern = Regex.Escape(prefix.TrimEnd('/')) + @"[\\/][A-Za-z0-9_./\\-]+";
            return Regex.Matches(Encoding.ASCII.GetString(bytes), pattern, RegexOptions.IgnoreCase).Cast<Match>().Select(m => m.Value.Replace('\\', '/').ToLowerInvariant()).Where(p => suffixes.Any(s => p.EndsWith(s, StringComparison.Ordinal))).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        static void Warn(AppearanceData result, string text)
        {
            if (!result.Warnings.Contains(text))
                result.Warnings.Add(text);
        }
    }
}
