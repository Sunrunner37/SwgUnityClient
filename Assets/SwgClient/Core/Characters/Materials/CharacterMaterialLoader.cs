using Swg.Client.Core.Assets.Formats.Dds;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Assets.Formats.Iff;
using Swg.Client.Core.Assets.Tre;

namespace Swg.Client.Core.Characters.Materials
{
    internal static class CharacterMaterialLoader
    {
        internal static string ResolveOwnerShader(TreAssetLoader loader, string shader, string ownerAppearance)
        {
            var proxy = IffNode.Form(IffNode.Parse(loader.Read(shader)), "OPST");
            if (proxy == null)
                return shader;
            var version = proxy.Children.First(node => node.Kind == "0000");
            string material = new ByteReader(version.Chunk("INFO")).CString();
            // Native OPST names use the owner's SAT basename followed by the proxy material name.
            return "shader/" + Path.GetFileNameWithoutExtension(ownerAppearance.Replace('\\', '/')) + "_" + material + ".sht";
        }

        // Both the base effect and its named variants use diffuse alpha as a MAIN/HUEB paint mask.
        internal static bool UsesTwoColor(string effect)
        {
            string name = Path.GetFileNameWithoutExtension(effect);
            return string.Equals(name, "h_color2", System.StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("h_color2_", System.StringComparison.OrdinalIgnoreCase);
        }

        internal static byte[] MainPaletteTint(TreAssetLoader loader, List<IffNode> nodes, IReadOnlyDictionary<string, short> own, IReadOnlyDictionary<string, short> owner) => PaletteTint(loader, nodes, own, owner, "MAIN");

        internal static byte[] PaletteTint(TreAssetLoader loader, List<IffNode> nodes, IReadOnlyDictionary<string, short> own, IReadOnlyDictionary<string, short> owner, string slot)
        {
            var factors = IffNode.Form(nodes, "TFAC");
            foreach (var pal in factors?.Children.Where(n => n.Tag == "PAL ") ?? Enumerable.Empty<IffNode>())
            {
                var r = new ByteReader(pal.Data);
                string variable = r.CString();
                byte scope = r.Byte();
                string target = Tag(r.Take(4), 0);
                string path = TreAssetLoader.Normalize(r.CString());
                int index = checked((int)r.U32());
                if (r.Remaining != 0)
                    throw new InvalidDataException("Unsupported shader palette binding");
                if (target != slot)
                    continue;
                var values = scope == 0 ? owner : own;
                string scoped = (scope == 0 ? "/shared_owner/" : "/private/") + variable;
                if (values.TryGetValue(scoped, out var actual))
                    index = actual;
                if (scope > 1)
                    Debug.LogWarning("Unknown shader customization scope " + scope + " for " + path + " (" + variable + "); default palette binding used.");
                return PaletteColor(loader, path, index);
            }

            // SSHT/TFNS holds fixed factors for channels without customization (e.g. the paralope's white HUEB).
            var fixedFactors = IffNode.Form(nodes, "TFNS")?.Children.FirstOrDefault(node => node.Tag == "0000");
            if (fixedFactors != null)
            {
                var reader = new ByteReader(fixedFactors.Data);
                while (reader.Remaining >= 8)
                {
                    string target = Tag(reader.Take(4), 0);
                    uint color = reader.U32();
                    if (target == slot) return new[] { (byte)(color >> 16), (byte)(color >> 8), (byte)color };
                }
            }
            return null;
        }

        // TRTS assigns texture-renderer output to specific shader slots in the skeletal mesh.
        // Fallback for recipes not handled by CharacterTextureRenderer: preserve the base MAIN palette color.
        internal static byte[] TextureRendererBaseTint(TreAssetLoader loader, IffNode meshVersion, int shaderIndex, IReadOnlyDictionary<string, short> own, IReadOnlyDictionary<string, short> owner)
        {
            var bindings = IffNode.Form(meshVersion.Children, "TRTS");
            if (bindings == null)
                return null;
            foreach (var binding in bindings.Children.Where(node => node.Tag == "TRT "))
            {
                var reader = new ByteReader(binding.Data);
                string path = reader.CString();
                int count = checked((int)reader.U32());
                bool applies = false;
                for (int i = 0; i < count; i++)
                {
                    int slot = checked((int)reader.U32());
                    string tag = Tag(reader.Take(4), 0);
                    applies |= slot == shaderIndex && tag == "MAIN";
                }

                if (!applies)
                    continue;
                var recipe = IffNode.Parse(loader.Read(path));
                var commands = IffNode.Form(recipe, "PCMS");
                if (commands == null)
                    continue;
                foreach (var command in commands.Children.Where(node => node.Kind == "PCMD"))
                {
                    var operations = IffNode.Form(command.Children, "TOPS");
                    if (operations == null)
                        continue;
                    foreach (var factor in operations.Children.Where(node => node.Tag == "STFP"))
                    {
                        var data = new ByteReader(factor.Data);
                        int material = data.U16();
                        string tag = Tag(data.Take(4), 0);
                        string palette = data.CString();
                        string variable = data.CString();
                        byte scope = data.Byte();
                        // Material zero is the recipe's base copy; later materials are overlays.
                        if (material != 0 || tag != "MAIN" || scope > 1)
                            continue;
                        var values = scope == 0 ? owner : own;
                        string name = (scope == 0 ? "/shared_owner/" : "/private/") + variable;
                        int index = values.TryGetValue(name, out var value) ? value : 0;
                        return PaletteColor(loader, palette, index);
                    }
                }
            }

            return null;
        }

        internal static byte[] PaletteColor(TreAssetLoader loader, string path, int index)
        {
            var bytes = loader.Read(path);
            if (bytes.Length < 24 || Encoding.ASCII.GetString(bytes, 0, 4) != "RIFF" || Encoding.ASCII.GetString(bytes, 8, 4) != "PAL " || Encoding.ASCII.GetString(bytes, 12, 4) != "data")
                throw new InvalidDataException("Unsupported character palette");
            int count = bytes[22] | bytes[23] << 8;
            if (count == 0 || bytes.Length < 24 + count * 4)
                throw new InvalidDataException("Invalid character palette: " + path);
            // Native palettes use their final entry for customization values outside the available range.
            if (index < 0 || index >= count)
                index = count - 1;
            int p = 24 + index * 4;
            return new[]
            {
                bytes[p],
                bytes[p + 1],
                bytes[p + 2]
            };
        }

        internal static TextureData ApplyTwoColor(TextureData source, byte[] main, byte[] trim)
        {
            var texture = new TextureData(source.Width, source.Height);
            for (int i = 0; i < source.Pixels.Length; i += 4)
            {
                float mask = source.Pixels[i + 3] / 255f;
                for (int channel = 0; channel < 3; channel++)
                    texture.Pixels[i + channel] = (byte)Mathf.RoundToInt(source.Pixels[i + channel] * Mathf.Lerp(main[channel], trim[channel], mask) / 255f);
                texture.Pixels[i + 3] = 255;
            }
            return texture;
        }

        static string Tag(byte[] bytes, int offset) => new string (new[] { (char)bytes[offset + 3], (char)bytes[offset + 2], (char)bytes[offset + 1], (char)bytes[offset] });
    }
}
