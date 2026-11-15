using Swg.Client.Core.Characters.Models;
using Swg.Client.Core.Assets.Formats.Sht;
using Swg.Client.Core.Assets.Formats.Cidm;
using Swg.Client.Core.Assets.Formats.Customization;
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

namespace Swg.Client.Core.Assets.Appearances
{
    /// <summary>Builds native skeletal appearances, including customization, wearables, occlusion, and vehicle bodies.</summary>
    public static class SkeletalAppearanceLoader
    {
        public static List<MeshPart> Load(TreAssetLoader loader, string objectPath) => LoadDetailed(loader, objectPath).Parts;
        public static SkeletalAppearanceData LoadDetailed(TreAssetLoader loader, string objectPath) => LoadDetailed(loader, new CharacterAppearance { TemplatePath = objectPath, Customization = PackedCustomization.Decode(Array.Empty<byte>()), Wearables = Array.Empty<WearableAppearance>() });
        public static List<MeshPart> Load(TreAssetLoader loader, CharacterAppearance appearance) => LoadDetailed(loader, appearance).Parts;
        public static SkeletalAppearanceData LoadDetailed(TreAssetLoader loader, CharacterAppearance appearance)
        {
            if (loader == null)
                throw new ArgumentNullException(nameof(loader));
            if (appearance == null)
                throw new ArgumentNullException(nameof(appearance));
            var result = new SkeletalAppearanceData();
            var ids = CidmFile.Parse(loader.Read("customization/customization_id_manager.iff"));
            var owner = new Dictionary<string, short>(appearance.Customization.ResolveNames(ids));
            var clientData = CharacterClientData.Load(loader, appearance.TemplatePath);
            foreach (var setting in clientData.Customization)
                if (!owner.ContainsKey(setting.Key)) owner[setting.Key] = setting.Value;
            var textures = new Dictionary<string, TextureData>(StringComparer.OrdinalIgnoreCase);
            string ownerAppearance = FindSkeletalAppearance(loader, appearance.TemplatePath, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
            if (!LoadObject(loader, appearance.TemplatePath, owner, owner, textures, result, ownerAppearance))
                throw new InvalidDataException("Template has no supported skeletal appearance");
            var occupiedSlots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var wearable in clientData.Wearables)
            {
                if (wearable.ObjectTemplate != null && LoadObject(loader, wearable.ObjectTemplate, wearable.Customization, owner, textures, result, ownerAppearance))
                    occupiedSlots.UnionWith(CharacterClientData.OccupiedSlots(loader, wearable.ObjectTemplate));
                foreach (string mesh in wearable.Meshes)
                    LoadMeshGenerator(loader, appearance.TemplatePath, ownerAppearance, mesh, wearable.Customization, owner, textures, result, ownerAppearance);
            }
            foreach (var item in appearance.Wearables)
            {
                try
                {
                    bool loaded = LoadObject(loader, item.TemplatePath, item.Customization.ResolveNames(ids), owner, textures, result, ownerAppearance);
                    if (loaded) occupiedSlots.UnionWith(CharacterClientData.OccupiedSlots(loader, item.TemplatePath));
                    if (!loaded && IsVisibleWearable(item.TemplatePath))
                        Debug.LogWarning("Unsupported wearable appearance: " + item.TemplatePath);
                }
                catch (Exception e)when (e is IOException || e is ArgumentException)
                {
                    if (IsVisibleWearable(item.TemplatePath))
                        Debug.LogWarning("Wearable could not be loaded: " + item.TemplatePath + ": " + e.Message);
                }
            }

            // Modesty is a fallback, not equipment: omit each mesh when clothing occupies its slot.
            if (ownerAppearance != null && appearance.TemplatePath.StartsWith("object/creature/player/", StringComparison.OrdinalIgnoreCase))
            {
                bool female = ownerAppearance.EndsWith("_f.sat", StringComparison.OrdinalIgnoreCase);
                bool male = ownerAppearance.EndsWith("_m.sat", StringComparison.OrdinalIgnoreCase);
                if ((female || male) && !occupiedSlots.Contains("pants1") && !occupiedSlots.Contains("pants2"))
                    LoadMeshGenerator(loader, appearance.TemplatePath, ownerAppearance, female ? "appearance/mesh/underwear_bottom_f.lmg" : "appearance/mesh/underwear_bottom_m.lmg", owner, owner, textures, result, ownerAppearance);
                if (female && !occupiedSlots.Contains("chest1") && !occupiedSlots.Contains("chest2"))
                    LoadMeshGenerator(loader, appearance.TemplatePath, ownerAppearance, "appearance/mesh/underwear_top_f.lmg", owner, owner, textures, result, ownerAppearance);
            }
            CharacterMeshOcclusion.Apply(result);
            if (VehicleBodyAppearance.TryLoad(loader, ownerAppearance, owner, out var body))
            {
                result.Parts.AddRange(body.Parts);
                result.Particles.AddRange(body.Particles);
                result.RiderSeat = body.RiderSeat;
            }
            return result;
        }

        static bool IsVisibleWearable(string path) => path.StartsWith("object/tangible/hair/", StringComparison.Ordinal) || path.StartsWith("object/tangible/wearables/", StringComparison.Ordinal) || (path.StartsWith("object/weapon/", StringComparison.Ordinal) && !path.Contains("/unarmed/"));
        static bool LoadObject(TreAssetLoader loader, string template, IReadOnlyDictionary<string, short> own, IReadOnlyDictionary<string, short> owner, Dictionary<string, TextureData> textures, SkeletalAppearanceData result, string ownerAppearance)
        {
            string satPath = FindSkeletalAppearance(loader, template, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
            if (satPath == null)
                return false; // Inventory/bank/datapad/default unarmed cache entries are not visible clothing.
            var sat = loader.Read(satPath);
            var meshes = References(sat, "appearance/", ".lmg").Concat(References(sat, "appearance/", ".mgn")).ToArray();
            if (meshes.Length == 0)
                return false;
            foreach (string mesh in meshes)
                LoadMeshGenerator(loader, template, satPath, mesh, own, owner, textures, result, ownerAppearance);

            return true;
        }

        static void LoadMeshGenerator(TreAssetLoader loader, string template, string satPath, string path, IReadOnlyDictionary<string, short> own, IReadOnlyDictionary<string, short> owner, Dictionary<string, TextureData> textures, SkeletalAppearanceData result, string ownerAppearance)
        {
            string mesh = path.EndsWith(".mgn", StringComparison.OrdinalIgnoreCase) ? path : References(loader.Read(path), "appearance/", ".mgn").FirstOrDefault();
            if (mesh == null)
                throw new InvalidDataException("No skeletal mesh LOD in " + path);
            LoadMesh(loader, template, satPath, mesh, own, owner, textures, result, ownerAppearance);
        }

        static string FindSkeletalAppearance(TreAssetLoader loader, string template, HashSet<string> seen)
        {
            if (seen.Count >= 16 || !seen.Add(template))
                return null;
            if (template.EndsWith(".sat", StringComparison.OrdinalIgnoreCase))
                return template;
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

        static void LoadMesh(TreAssetLoader loader, string template, string satPath, string path, IReadOnlyDictionary<string, short> own, IReadOnlyDictionary<string, short> owner, Dictionary<string, TextureData> textures, SkeletalAppearanceData result, string ownerAppearance)
        {
            var skmg = IffNode.Form(IffNode.Parse(loader.Read(path)), "SKMG") ?? throw new InvalidDataException("Not a skeletal mesh");
            var version = skmg.Children.FirstOrDefault(n => n.Kind == "0003" || n.Kind == "0004") ?? throw new InvalidDataException("Unsupported skeletal mesh version");
            byte[] positions = version.Chunk("POSN");
            byte[] normals = version.Chunk("NORM");
            if (positions.Length % 12 != 0 || normals.Length % 12 != 0)
                throw new InvalidDataException("Invalid skeletal geometry arrays");
            MeshBlendTargets.Apply(version, positions, normals, own, owner);
            var pose = SkeletalPose.Load(loader, satPath, path, ownerAppearance);
            int shaderIndex = 0;
            foreach (var psdt in version.Children.Where(n => n.Kind == "PSDT"))
            {
                string shader = CharacterMaterialLoader.ResolveOwnerShader(loader, new ByteReader(psdt.Chunk("NAME")).CString(), ownerAppearance);
                byte[] shaderBytes = loader.Read(shader);
                var shaderNodes = IffNode.Parse(shaderBytes);
                if (!ShtFile.TryGetMainTexture(shaderNodes, out var diffusePath))
                {
                    diffusePath = References(shaderBytes, "texture/", ".dds").FirstOrDefault(p => !p.EndsWith("_cn.dds") && !p.EndsWith("_n.dds") && !p.EndsWith("_spec.dds") && !p.EndsWith("_sm.dds") && !p.Contains("aniso_lookup"));
                    if (diffusePath == null)
                        throw new InvalidDataException("Shader has no MAIN diffuse texture: " + shader);
                    Debug.LogWarning("Shader uses an unrecognized texture-slot layout: " + shader);
                }

                byte[] tint = null;
                if (!CharacterTextureRenderer.TryBake(loader, version, shaderIndex, own, owner, textures, out var diffuse))
                {
                    if (!textures.TryGetValue(diffusePath, out diffuse))
                    {
                        diffuse = TextureData.Load(loader, diffusePath);
                        textures.Add(diffusePath, diffuse);
                    }
                    tint = CharacterMaterialLoader.TextureRendererBaseTint(loader, version, shaderIndex, own, owner) ?? CharacterMaterialLoader.MainPaletteTint(loader, shaderNodes, own, owner);
                }
                var trim = CharacterMaterialLoader.PaletteTint(loader, shaderNodes, own, owner, "HUEB");
                string effect = IffNode.References(shaderBytes, "effect/", ".eft").FirstOrDefault();
                if (trim != null && effect != null && Path.GetFileName(effect).StartsWith("h_color2_", StringComparison.OrdinalIgnoreCase))
                {
                    diffuse = CharacterMaterialLoader.ApplyTwoColor(diffuse, tint ?? new byte[] { 255, 255, 255 }, trim);
                    tint = null;
                }
                shaderIndex++;
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
                var bindVertices = new Vector3[count];
                var bindNormals = new Vector3[count];
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
                    bindVertices[i] = new Vector3(rp.Float(), rp.Float(), rp.Float());
                    bindNormals[i] = new Vector3(rn.Float(), rn.Float(), rn.Float());
                    vertices[i] = pose.TransformPosition(pi, bindVertices[i]);
                    ns[i] = pose.TransformNormal(pi, bindNormals[i]).normalized;
                    uvs[i] = new Vector2(uv.Float(), uv.Float());
                    sourceIndices[i] = pi;
                }

                var triangles = new List<int>();
                var occlusion = new List<int>();
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
                        occlusion.Add(stride == 14 ? r.U16() : -1);
                        for (int v = 0; v < 3; v++)
                        {
                            int index = checked((int)r.U32());
                            if (index >= count)
                                throw new InvalidDataException("Skeletal triangle index out of bounds");
                            triangles.Add(index);
                        }
                    }
                }

                result.Parts.Add(new MeshPart { Vertices = vertices, Normals = ns, Uvs = uvs, Triangles = triangles.ToArray(), Diffuse = diffuse, Tint = tint, Skinning = pose.CreateSkin(bindVertices, bindNormals, sourceIndices), UseCharacterShader = true, AlphaCutout = template.Contains("/hair/") || path.Contains("/hair/") });
                result.Sources.Add(new CachedMeshPartSource { ObjectTemplate = template, MeshPath = path, ShaderPath = shader, PositionIndices = sourceIndices, TriangleOcclusionCombinations = occlusion.ToArray(), MeshVersion = version });
            }
        }

        static List<string> References(byte[] bytes, string prefix, string suffix)
        {
            string text = Encoding.ASCII.GetString(bytes);
            string pattern = Regex.Escape(prefix.TrimEnd('/')) + @"[\\/][A-Za-z0-9_./\\-]+" + Regex.Escape(suffix);
            return Regex.Matches(text, pattern, RegexOptions.IgnoreCase).Cast<Match>().Select(m => TreAssetLoader.Normalize(m.Value)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }
    }
}
