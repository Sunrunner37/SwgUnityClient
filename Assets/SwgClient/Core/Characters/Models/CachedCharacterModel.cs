using Swg.Client.Core.Assets.Formats.Cidm;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Assets.Formats.Dds;
using Swg.Client.Core.Assets.Formats.Iff;
using Swg.Client.Core.Assets.Tre;
using Swg.Client.Core.Characters.Animation;
using Swg.Client.Core.Characters.Appearance;
using Swg.Client.Core.Characters.Materials;
using Swg.Client.Core.Geometry;

namespace Swg.Client.Core.Characters.Models
{
    /// <summary>
    /// Builds original player/hair/wearable skeletal meshes using the native .ca
    /// template and customization values. Does not invent an animation pose.
    /// </summary>
    public static class CachedCharacterModel
    {
        public static List<MeshPart> Load(TreAssetLoader loader, CachedCharacterAppearance appearance) => LoadDetailed(loader, appearance).Parts;
        public static CachedCharacterModelResult LoadDetailed(TreAssetLoader loader, CachedCharacterAppearance appearance)
        {
            if (loader == null)
                throw new ArgumentNullException(nameof(loader));
            if (appearance == null)
                throw new ArgumentNullException(nameof(appearance));
            var result = new CachedCharacterModelResult();
            var ids = CidmFile.Parse(loader.Read("customization/customization_id_manager.iff"));
            var owner = appearance.Customization.ResolveNames(ids);
            var textures = new Dictionary<string, TextureData>(StringComparer.OrdinalIgnoreCase);
            if (!appearance.FromCache && !string.IsNullOrEmpty(appearance.Diagnostic))
                Warn(result, appearance.Diagnostic);
            if (!LoadObject(loader, appearance.TemplatePath, owner, owner, textures, result))
                throw new InvalidDataException("Player template has no supported skeletal appearance");
            foreach (var item in appearance.Wearables)
            {
                try
                {
                    bool loaded = LoadObject(loader, item.TemplatePath, item.Customization.ResolveNames(ids), owner, textures, result);
                    if (!loaded && IsVisibleWearable(item.TemplatePath))
                        Warn(result, "Unsupported wearable appearance: " + item.TemplatePath);
                }
                catch (Exception e)when (e is IOException || e is ArgumentException)
                {
                    if (IsVisibleWearable(item.TemplatePath))
                        Warn(result, "Wearable could not be loaded: " + item.TemplatePath + ": " + e.Message);
                }
            }

            return result;
        }

        static bool IsVisibleWearable(string path) => path.StartsWith("object/tangible/hair/", StringComparison.Ordinal) || path.StartsWith("object/tangible/wearables/", StringComparison.Ordinal) || (path.StartsWith("object/weapon/", StringComparison.Ordinal) && !path.Contains("/unarmed/"));
        static bool LoadObject(TreAssetLoader loader, string template, IReadOnlyDictionary<string, short> own, IReadOnlyDictionary<string, short> owner, Dictionary<string, TextureData> textures, CachedCharacterModelResult result)
        {
            string satPath = FindSkeletalAppearance(loader, template, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
            if (satPath == null)
                return false; // Inventory/bank/datapad/default unarmed cache entries are not visible clothing.
            var sat = loader.Read(satPath);
            var lmgs = References(sat, "appearance/", ".lmg");
            if (lmgs.Count == 0)
                return false;
            foreach (string lmg in lmgs)
            {
                string mgn = References(loader.Read(lmg), "appearance/", ".mgn").FirstOrDefault();
                if (mgn == null)
                    throw new InvalidDataException("No skeletal mesh LOD in " + lmg);
                LoadMesh(loader, template, satPath, mgn, own, owner, textures, result);
            }

            return true;
        }

        static string FindSkeletalAppearance(TreAssetLoader loader, string template, HashSet<string> seen)
        {
            if (seen.Count >= 16 || !seen.Add(template))
                return null;
            byte[] data = loader.Read(template);
            string own = References(data, "appearance/", ".sat").FirstOrDefault();
            if (own != null)
                return own;
            // Appearance may be inherited by a shared tangible template.
            foreach (string parent in References(data, "object/", ".iff"))
            {
                string inherited = FindSkeletalAppearance(loader, parent, seen);
                if (inherited != null)
                    return inherited;
            }

            return null;
        }

        static void LoadMesh(TreAssetLoader loader, string template, string satPath, string path, IReadOnlyDictionary<string, short> own, IReadOnlyDictionary<string, short> owner, Dictionary<string, TextureData> textures, CachedCharacterModelResult result)
        {
            var skmg = IffNode.Form(IffNode.Parse(loader.Read(path)), "SKMG") ?? throw new InvalidDataException("Not a skeletal mesh");
            var version = skmg.Children.FirstOrDefault(n => n.Kind == "0004") ?? throw new InvalidDataException("Unsupported skeletal mesh version");
            byte[] positions = version.Chunk("POSN");
            byte[] normals = version.Chunk("NORM");
            if (positions.Length % 12 != 0 || normals.Length % 12 != 0)
                throw new InvalidDataException("Invalid skeletal geometry arrays");
            MeshBlendTargets.Apply(version, positions, normals, own, owner);
            var pose = SkeletalPose.Load(loader, satPath, path);
            if (version.Children.Any(n => n.Kind == "TRTS"))
                Warn(result, "Native texture-renderer compositing is not applied to " + path + "; base textures and palette colors are used.");
            foreach (var psdt in version.Children.Where(n => n.Kind == "PSDT"))
            {
                string shader = new ByteReader(psdt.Chunk("NAME")).CString();
                byte[] shaderBytes = loader.Read(shader);
                var shaderNodes = IffNode.Parse(shaderBytes);
                string diffusePath = CharacterMaterialLoader.MainTexture(shaderNodes);
                if (diffusePath == null)
                {
                    diffusePath = References(shaderBytes, "texture/", ".dds").FirstOrDefault(p => !p.EndsWith("_cn.dds") && !p.EndsWith("_n.dds") && !p.EndsWith("_spec.dds") && !p.EndsWith("_sm.dds") && !p.Contains("aniso_lookup"));
                    if (diffusePath == null)
                        throw new InvalidDataException("Shader has no MAIN diffuse texture: " + shader);
                    Warn(result, "Shader uses an unrecognized texture-slot layout: " + shader);
                }

                if (!textures.TryGetValue(diffusePath, out var diffuse))
                {
                    diffuse = TextureData.Load(loader, diffusePath);
                    textures.Add(diffusePath, diffuse);
                }

                byte[] tint = CharacterMaterialLoader.MainPaletteTint(loader, shaderNodes, own, owner, message => Warn(result, message));
                var pidx = new ByteReader(psdt.Chunk("PIDX"));
                int count = checked((int)pidx.U32());
                if (count > 100000 || pidx.Remaining != count * 4)
                    throw new InvalidDataException("Invalid skeletal mesh vertex count");
                var nidx = new ByteReader(psdt.Chunk("NIDX"));
                if (nidx.Remaining != count * 4)
                    throw new InvalidDataException("Invalid skeletal mesh normal count");
                var texcoords = IffNode.Form(psdt.Children, "TCSF")?.Chunk("TCSD") ?? throw new InvalidDataException("Missing skeletal texture coordinates");
                if (texcoords.Length != count * 8)
                    throw new InvalidDataException("Unsupported skeletal UV layout");
                var uv = new ByteReader(texcoords);
                var vertices = new Vector3[count];
                var ns = new Vector3[count];
                var uvs = new Vector2[count];
                var sourceIndices = new int[count];
                for (int i = 0; i < count; i++)
                {
                    int pi = checked((int)pidx.U32());
                    int ni = checked((int)nidx.U32());
                    if (pi < 0 || pi >= positions.Length / 12 || ni < 0 || ni >= normals.Length / 12)
                        throw new InvalidDataException("Skeletal source index out of bounds");
                    var rp = new ByteReader(positions)
                    {
                        Position = pi * 12
                    };
                    var rn = new ByteReader(normals)
                    {
                        Position = ni * 12
                    };
                    vertices[i] = pose.TransformPosition(pi, new Vector3(rp.Float(), rp.Float(), rp.Float()));
                    ns[i] = pose.TransformNormal(pi, new Vector3(rn.Float(), rn.Float(), rn.Float())).normalized;
                    uvs[i] = new Vector2(uv.Float(), uv.Float());
                    sourceIndices[i] = pi;
                }

                var triangles = new List<int>();
                var prim = IffNode.Form(psdt.Children, "PRIM") ?? throw new InvalidDataException("Missing skeletal mesh primitives");
                foreach (var n in prim.Children)
                {
                    if (n.Tag != "OITL" && n.Tag != "ITL ")
                        continue;
                    var r = new ByteReader(n.Data);
                    int triangleCount = checked((int)r.U32());
                    int stride = n.Tag == "OITL" ? 14 : 12;
                    if (triangleCount > 200000 || r.Remaining != triangleCount * stride)
                        throw new InvalidDataException("Invalid skeletal mesh triangles");
                    for (int i = 0; i < triangleCount; i++)
                    {
                        if (stride == 14)
                            r.U16();
                        for (int v = 0; v < 3; v++)
                        {
                            int index = checked((int)r.U32());
                            if (index >= count)
                                throw new InvalidDataException("Skeletal triangle index out of bounds");
                            triangles.Add(index);
                        }
                    }
                }

                result.Parts.Add(new MeshPart { Vertices = vertices, Normals = ns, Uvs = uvs, Triangles = triangles.ToArray(), Diffuse = diffuse, Tint = tint });
                result.Sources.Add(new CachedMeshPartSource { ObjectTemplate = template, MeshPath = path, ShaderPath = shader, PositionIndices = sourceIndices, MeshVersion = version });
            }
        }

        static void Warn(CachedCharacterModelResult result, string message)
        {
            if (!result.Warnings.Contains(message))
                result.Warnings.Add(message);
        }

        static List<string> References(byte[] bytes, string prefix, string suffix)
        {
            string text = Encoding.ASCII.GetString(bytes);
            string pattern = Regex.Escape(prefix.TrimEnd('/')) + @"[\\/][A-Za-z0-9_./\\-]+" + Regex.Escape(suffix);
            return Regex.Matches(text, pattern, RegexOptions.IgnoreCase).Cast<Match>().Select(m => TreAssetLoader.Normalize(m.Value)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }
    }
}
