using Swg.Client.Core.Assets.Formats.Sht;
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

namespace Swg.Client.Core.Assets.Appearances
{
    /// <summary>Decodes MESH shader primitives, vertex layouts and materials. Appearance traversal lives in AppearanceLoader.</summary>
    internal static class MeshAppearanceReader
    {
        sealed class AssetCache
        {
            public readonly Dictionary<string, TextureData> Textures = new Dictionary<string, TextureData>(StringComparer.OrdinalIgnoreCase);
            public readonly Dictionary<string, TextureData> ColoredTextures = new Dictionary<string, TextureData>(StringComparer.OrdinalIgnoreCase);
            public readonly Dictionary<string, EftFile> Effects = new Dictionary<string, EftFile>(StringComparer.OrdinalIgnoreCase);
        }

        static readonly ConditionalWeakTable<TreAssetLoader, AssetCache> CachedAssets = new ConditionalWeakTable<TreAssetLoader, AssetCache>();
        static readonly IReadOnlyDictionary<string, short> Defaults = new Dictionary<string, short>();
        public static void Load(TreAssetLoader loader, string path, IffNode mesh, Matrix4x4 matrix, AppearanceData result, IReadOnlyDictionary<string, short> customization = null)
        {
            customization = customization ?? Defaults;
            var hardpoints = IffNode.Form(mesh.Children, "HPTS");
            if (hardpoints != null)
                foreach (var hardpoint in hardpoints.Children.Where(node => node.Tag == "HPNT"))
                {
                    var reader = new ByteReader(hardpoint.Data);
                    var local = Matrix4x4.identity;
                    for (int row = 0; row < 3; row++)
                        for (int column = 0; column < 4; column++)
                            local[row, column] = reader.Float();
                    result.Hardpoints[reader.CString()] = matrix * local;
                }
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
                bool alphaBlend = false;
                bool fullbright = false;
                Color emission = Color.black;
                bool ambientVertexColor = false;
                ShtFile.TextureSwitcher switcher = null;
                TextureData[] frames = null;
                byte[] tint = null;
                TextureData texture = null;
                try
                {
                    var shaderBytes = loader.Read(shader);
                    var shaderNodes = IffNode.Parse(shaderBytes);
                    switcher = ShtFile.ReadTextureSwitcher(shaderNodes);
                    if (switcher != null)
                    {
                        frames = switcher.Frames.Select(frame => Texture(loader, frame)).ToArray();
                        shaderBytes = loader.Read(switcher.BaseShader);
                        shaderNodes = IffNode.Parse(shaderBytes);
                    }
                    if (!ShtFile.TryGetMainTexture(shaderNodes, out diffuse))
                        diffuse = References(shaderBytes, "texture/", new[] { ".dds" }).FirstOrDefault(p => !p.EndsWith("_n.dds") && !p.EndsWith("_cn.dds") && !p.EndsWith("_spec.dds") && !p.EndsWith("_sm.dds"));
                    if (frames != null)
                    {
                        diffuse = switcher.Frames[0];
                        texture = frames[0];
                    }
                    else if (diffuse != null)
                        texture = Texture(loader, diffuse);
                    ShtFile.TryGetEmission(shaderNodes, out emission);
                    tint = CharacterMaterialLoader.MainPaletteTint(loader, shaderNodes, customization, customization);
                    var effect = References(shaderBytes, "effect/", new[] { ".eft" }).FirstOrDefault();
                    cutout = effect != null && Effect(loader, effect).AlphaTestEnabled;
                    alphaBlend = effect != null && Effect(loader, effect).AlphaBlendEnabled;
                    ambientVertexColor = effect != null && Effect(loader, effect).VertexColorAmbientOnly;
                    fullbright = effect != null && Path.GetFileName(effect).Contains("emis_full");
                    var trim = CharacterMaterialLoader.PaletteTint(loader, shaderNodes, customization, customization, "HUEB");
                    if (texture != null && trim != null && effect != null && CharacterMaterialLoader.UsesTwoColor(effect))
                    {
                        texture = TwoColorTexture(loader, shader, texture, tint ?? new byte[] { 255, 255, 255, 255 }, trim);
                        tint = null;
                    }
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

                result.Parts.Add(new MeshPart { Vertices = positions, Normals = normals, Uvs = uvs, Triangles = indices, Diffuse = texture, Tint = tint, AlphaCutout = cutout, AlphaBlend = alphaBlend, Fullbright = fullbright, Emission = emission, VertexColorAmbientOnly = ambientVertexColor, VertexColors = colors,
                    DiffuseFrames = frames, MinimumFrameSeconds = switcher?.MinimumSeconds ?? 0, MaximumFrameSeconds = switcher?.MaximumSeconds ?? 0 });
                result.Sources.Add(new StaticMeshSource { AppearancePath = path, ShaderPath = shader, DiffusePath = diffuse });
            }
        }

        // Native h_color2 shaders interpolate MAIN/HUEB using diffuse alpha, which is a paint mask, not transparency.
        static TextureData TwoColorTexture(TreAssetLoader loader, string shader, TextureData source, byte[] main, byte[] trim)
        {
            var cache = CachedAssets.GetValue(loader, _ => new AssetCache());
            string key = shader + "#" + BitConverter.ToString(main) + "/" + BitConverter.ToString(trim);
            lock (cache)
            {
                if (cache.ColoredTextures.TryGetValue(key, out var texture)) return texture;
                texture = CharacterMaterialLoader.ApplyTwoColor(source, main, trim);
                cache.ColoredTextures.Add(key, texture);
                return texture;
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

        static List<string> References(byte[] bytes, string prefix, string[] suffixes) => suffixes.SelectMany(suffix => IffNode.References(bytes, prefix, suffix)).Distinct().ToList();
        static void Warn(AppearanceData result, string text)
        {
            if (!result.Warnings.Contains(text))
                result.Warnings.Add(text);
        }
    }
}
