using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Assets.Formats.Dds;
using Swg.Client.Core.Assets.Formats.Eft;
using Swg.Client.Core.Assets.Formats.Iff;
using Swg.Client.Core.Assets.Tre;
using Swg.Client.Core.Characters.Materials;
using Swg.Client.Core.Geometry;

namespace Swg.Client.Core.World
{
    /// <summary>Loads original APT/DTLA/MESH/PRTO/CMPA render geometry without extracting TRE members.</summary>
    public static class StaticModel
    {
        sealed class AssetCache
        {
            public readonly Dictionary<string, TextureData> Textures = new Dictionary<string, TextureData>(StringComparer.OrdinalIgnoreCase);
            public readonly Dictionary<string, EftFile> Effects = new Dictionary<string, EftFile>(StringComparer.OrdinalIgnoreCase);
        }

        static readonly ConditionalWeakTable<TreAssetLoader, AssetCache> CachedAssets = new ConditionalWeakTable<TreAssetLoader, AssetCache>();
        static readonly IReadOnlyDictionary<string, short> Defaults = new Dictionary<string, short>();
        public static List<MeshPart> Load(TreAssetLoader loader, string path) => LoadDetailed(loader, path).Parts;
        public static StaticModelResult LoadDetailed(TreAssetLoader loader, string path)
        {
            var result = new StaticModelResult();
            LoadAppearance(loader, TreAssetLoader.Normalize(path), Matrix4x4.identity, result, new HashSet<string>(StringComparer.OrdinalIgnoreCase), 0);
            bool first = true;
            foreach (var part in result.Parts)
                foreach (var p in part.Vertices)
                {
                    if (first)
                    {
                        result.BoundsMin = p;
                        result.BoundsMax = p;
                        first = false;
                    }

                    result.BoundsMin = Vector3.Min(result.BoundsMin, p);
                    result.BoundsMax = Vector3.Max(result.BoundsMax, p);
                    result.Radius = Math.Max(result.Radius, p.magnitude);
                }

            return result;
        }

        static void LoadAppearance(TreAssetLoader loader, string path, Matrix4x4 transform, StaticModelResult result, HashSet<string> stack, int depth)
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
                    var appearances = References(bytes, "appearance/", new[] { ".apt", ".lod", ".msh", ".pob", ".cmp" });
                    if (appearances.Count > 0)
                    {
                        LoadAppearance(loader, appearances[0], transform, result, stack, depth + 1);
                        return;
                    }

                    if (path.StartsWith("object/cell/", StringComparison.Ordinal) || path.StartsWith("object/intangible/", StringComparison.Ordinal))
                        return;
                    foreach (var inherited in References(bytes, "object/", new[] { ".iff" }).Where(p => p != path))
                    {
                        int count = result.Parts.Count;
                        LoadAppearance(loader, inherited, transform, result, stack, depth + 1);
                        if (result.Parts.Count != count)
                            return;
                    }

                    return;
                }

                if (root.Kind == "APT ")
                {
                    var version = root.Children.First(n => n.Tag == "FORM");
                    LoadAppearance(loader, AppearancePath(new ByteReader(version.Chunk("NAME")).CString()), transform, result, stack, depth + 1);
                }
                else if (root.Kind == "DTLA")
                {
                    var version = root.Children.First(n => n.Tag == "FORM");
                    var data = version.Children.Single(n => n.Kind == "DATA");
                    var child = data.Children.LastOrDefault(n => n.Tag == "CHLD") ?? throw new InvalidDataException("Empty detail appearance");
                    var reader = new ByteReader(child.Data);
                    reader.U32();
                    LoadAppearance(loader, AppearancePath(reader.CString()), transform, result, stack, depth + 1);
                }
                else if (root.Kind == "MESH")
                    LoadMesh(loader, path, root, transform, result);
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
                            LoadChild(loader, AppearancePath(appearance), transform, result, stack, depth + 1);
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
                        LoadChild(loader, appearance, transform * local, result, stack, depth + 1);
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

        static void LoadChild(TreAssetLoader loader, string path, Matrix4x4 matrix, StaticModelResult result, HashSet<string> stack, int depth)
        {
            try
            {
                LoadAppearance(loader, path, matrix, result, stack, depth);
            }
            catch (Exception e)when (e is IOException || e is InvalidOperationException || e is ArgumentException)
            {
                Warn(result, "Appearance could not be loaded " + path + ": " + e.Message);
            }
        }

        static void LoadMesh(TreAssetLoader loader, string path, IffNode mesh, Matrix4x4 matrix, StaticModelResult result)
        {
            // Inverse-transpose preserves normals under non-uniform scale. Compute once per mesh.
            var normalMatrix = Math.Abs(matrix.determinant) < .000001f ? Matrix4x4.identity : matrix.inverse.transpose;
            var version = mesh.Children.First(n => n.Tag == "FORM");
            var sps = version.Children.SingleOrDefault(n => n.Kind == "SPS ") ?? throw new InvalidDataException("Missing mesh shader primitives");
            var primitiveVersion = sps.Children.First(n => n.Tag == "FORM");
            foreach (var group in primitiveVersion.Children.Where(n => n.Tag == "FORM"))
            {
                if (result.Parts.Count > 4096)
                    throw new InvalidDataException("Appearance part limit");
                string shader = TreAssetLoader.Normalize(new ByteReader(group.Chunk("NAME")).CString());
                var vertexArray = IffNode.Form(group.Children, "VTXA") ?? throw new InvalidDataException("Missing vertex array");
                var vertexVersion = vertexArray.Children.First(n => n.Tag == "FORM");
                if (vertexVersion.Kind != "0003")
                    throw new InvalidDataException("Unsupported vertex array version " + vertexVersion.Kind);
                var info = new ByteReader(vertexVersion.Chunk("INFO"));
                uint flags = info.U32();
                int count = checked((int)info.U32());
                var bytes = vertexVersion.Chunk("DATA");
                if (count < 1 || count > 2000000 || bytes.Length % count != 0)
                    throw new InvalidDataException("Invalid static vertex count/stride");
                int stride = bytes.Length / count;
                if (stride < 12 || stride > 256 || (flags & 1) == 0 || (flags & 2) != 0)
                    throw new InvalidDataException("Unsupported static vertex layout");
                bool hasNormals = (flags & 4) != 0;
                int cursor = 12 + (hasNormals ? 12 : 0);
                int colorOffset = (flags & 8) != 0 ? cursor : -1;
                if ((flags & 8) != 0)
                    cursor += 4;
                if ((flags & 16) != 0)
                    cursor += 4;
                int textureSets = (int)((flags >> 8) & 15);
                int uvOffset = textureSets > 0 ? cursor : -1;
                if (hasNormals && stride < 24)
                    throw new InvalidDataException("Truncated static normal data");
                if (uvOffset >= 0 && uvOffset + 8 > stride)
                    throw new InvalidDataException("Truncated static texture coordinates");
                var positions = new Vector3[count];
                var normals = new Vector3[count];
                var uvs = new Vector2[count];
                byte[] colors = colorOffset < 0 ? null : new byte[count * 4];
                for (int i = 0; i < count; i++)
                {
                    int offset = i * stride;
                    positions[i] = matrix.MultiplyPoint3x4(Vector(bytes, offset));
                    if (hasNormals)
                        normals[i] = Normalize(normalMatrix.MultiplyVector(Vector(bytes, offset + 12)));
                    if (uvOffset >= 0)
                        uvs[i] = new Vector2(Number(bytes, offset + uvOffset), Number(bytes, offset + uvOffset + 4));
                    if (colors != null)
                    {
                        colors[i * 4] = bytes[offset + colorOffset + 2];
                        colors[i * 4 + 1] = bytes[offset + colorOffset + 1];
                        colors[i * 4 + 2] = bytes[offset + colorOffset];
                        colors[i * 4 + 3] = bytes[offset + colorOffset + 3];
                    }
                }

                var indexNode = Descendants(group).SingleOrDefault(n => n.Tag == "INDX") ?? throw new InvalidDataException("Missing mesh indices");
                var indexReader = new ByteReader(indexNode.Data);
                int indexCount = checked((int)indexReader.U32());
                if (indexCount < 0 || indexCount > 6000000 || indexCount % 3 != 0)
                    throw new InvalidDataException("Invalid triangle list");
                int width = indexReader.Remaining == indexCount * 2 ? 2 : indexReader.Remaining == indexCount * 4 ? 4 : 0;
                if (width == 0)
                    throw new InvalidDataException("Invalid mesh index format");
                var indices = new int[indexCount];
                for (int i = 0; i < indices.Length; i++)
                {
                    indices[i] = checked((int)(width == 2 ? indexReader.U16() : indexReader.U32()));
                    if (indices[i] >= count)
                        throw new InvalidDataException("Mesh index out of range");
                }

                if (matrix.determinant < 0)
                    for (int i = 0; i < indices.Length; i += 3)
                    {
                        int swap = indices[i + 1];
                        indices[i + 1] = indices[i + 2];
                        indices[i + 2] = swap;
                    }

                if (!hasNormals)
                    BuildNormals(positions, normals, indices);
                string diffuse = null;
                bool cutout = false;
                byte[] tint = null;
                TextureData texture = null;
                try
                {
                    var shaderBytes = loader.Read(shader);
                    var shaderNodes = IffNode.Parse(shaderBytes);
                    diffuse = CharacterMaterialLoader.MainTexture(shaderNodes);
                    if (diffuse == null)
                        diffuse = References(shaderBytes, "texture/", new[] { ".dds" }).FirstOrDefault(p => !p.EndsWith("_n.dds") && !p.EndsWith("_cn.dds") && !p.EndsWith("_spec.dds") && !p.EndsWith("_sm.dds"));
                    if (diffuse != null)
                        texture = Texture(loader, diffuse);
                    tint = CharacterMaterialLoader.MainPaletteTint(loader, shaderNodes, Defaults, Defaults, message => Warn(result, message));
                    var effect = References(shaderBytes, "effect/", new[] { ".eft" }).FirstOrDefault();
                    cutout = effect != null && Effect(loader, effect).AlphaTestEnabled;
                }
                catch (Exception e)when (e is IOException || e is ArgumentException)
                {
                    Warn(result, "Static shader fallback " + shader + ": " + e.Message);
                }

                if (texture == null)
                {
                    texture = White();
                    Warn(result, "No supported MAIN texture: " + shader);
                }

                result.Parts.Add(new MeshPart { Vertices = positions, Normals = normals, Uvs = uvs, Triangles = indices, Diffuse = texture, Tint = tint });
                result.Sources.Add(new StaticMeshSource { AppearancePath = path, ShaderPath = shader, DiffusePath = diffuse, AlphaCutoutHint = cutout, VertexColors = colors });
            }
        }

        static TextureData Texture(TreAssetLoader loader, string path)
        {
            var cache = CachedAssets.GetValue(loader, _ => new AssetCache());
            lock (cache)
            {
                if (!cache.Textures.TryGetValue(path, out var texture))
                {
                    texture = TextureData.Load(loader, path);
                    cache.Textures.Add(path, texture);
                }

                return texture;
            }
        }

        static EftFile Effect(TreAssetLoader loader, string path)
        {
            var cache = CachedAssets.GetValue(loader, _ => new AssetCache());
            lock (cache)
            {
                if (!cache.Effects.TryGetValue(path, out var effect))
                {
                    effect = EftFile.Parse(loader.Read(path));
                    cache.Effects.Add(path, effect);
                }
                return effect;
            }
        }

        static TextureData White()
        {
            var texture = new TextureData(1, 1);
            for (int i = 0; i < 4; i++)
                texture.Pixels[i] = 255;
            return texture;
        }

        static IEnumerable<IffNode> Descendants(IffNode node)
        {
            foreach (var child in node.Children)
            {
                yield return child;
                foreach (var nested in Descendants(child))
                    yield return nested;
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

        static float Number(byte[] bytes, int offset)
        {
            float value = BitConverter.ToSingle(bytes, offset);
            if (float.IsNaN(value) || float.IsInfinity(value))
                throw new InvalidDataException("Invalid mesh coordinate");
            return value;
        }

        static Vector3 Vector(byte[] bytes, int offset) => new Vector3(Number(bytes, offset), Number(bytes, offset + 4), Number(bytes, offset + 8));
        static Vector3 Normalize(Vector3 v)
        {
            float length = v.magnitude;
            return length > .000001f ? v / length : new Vector3(0, 1, 0);
        }

        static void BuildNormals(Vector3[] positions, Vector3[] normals, int[] indices)
        {
            for (int i = 0; i < indices.Length; i += 3)
            {
                var a = positions[indices[i]];
                var b = positions[indices[i + 1]];
                var c = positions[indices[i + 2]];
                var u = b - a;
                var v = c - a;
                var n = Vector3.Cross(u, v);
                for (int j = 0; j < 3; j++)
                {
                    int at = indices[i + j];
                    normals[at] += n;
                }
            }

            for (int i = 0; i < normals.Length; i++)
                normals[i] = Normalize(normals[i]);
        }

        static void Warn(StaticModelResult result, string text)
        {
            if (!result.Warnings.Contains(text))
                result.Warnings.Add(text);
        }
    }
}
