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

        internal static byte[] MainPaletteTint(TreAssetLoader loader, List<IffNode> nodes, IReadOnlyDictionary<string, short> own, IReadOnlyDictionary<string, short> owner)
        {
            var factors = IffNode.Form(nodes, "TFAC");
            if (factors == null)
                return null;
            foreach (var pal in factors.Children.Where(n => n.Tag == "PAL "))
            {
                var r = new ByteReader(pal.Data);
                string variable = r.CString();
                byte scope = r.Byte();
                string target = Tag(r.Take(4), 0);
                string path = TreAssetLoader.Normalize(r.CString());
                int index = checked((int)r.U32());
                if (r.Remaining != 0)
                    throw new InvalidDataException("Unsupported shader palette binding");
                if (target != "MAIN")
                    continue;
                var values = scope == 0 ? owner : own;
                string scoped = (scope == 0 ? "/shared_owner/" : "/private/") + variable;
                if (values.TryGetValue(scoped, out var actual))
                    index = actual;
                if (scope > 1)
                    Debug.LogWarning("Unknown shader customization scope " + scope + " for " + path + " (" + variable + "); default palette binding used.");
                return PaletteColor(loader, path, index);
            }

            return null;
        }

        // TRTS assigns texture-renderer output to specific shader slots in the skeletal mesh.
        // Until layered baking is supported, preserve the base MAIN palette color on that slot.
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

        static byte[] PaletteColor(TreAssetLoader loader, string path, int index)
        {
            var bytes = loader.Read(path);
            if (bytes.Length < 24 || Encoding.ASCII.GetString(bytes, 0, 4) != "RIFF" || Encoding.ASCII.GetString(bytes, 8, 4) != "PAL " || Encoding.ASCII.GetString(bytes, 12, 4) != "data")
                throw new InvalidDataException("Unsupported character palette");
            int count = bytes[22] | bytes[23] << 8;
            if (count == 0 || bytes.Length < 24 + count * 4 || index < 0 || index >= count)
                throw new InvalidDataException("Character palette index out of bounds");
            int p = 24 + index * 4;
            return new[]
            {
                bytes[p],
                bytes[p + 1],
                bytes[p + 2]
            };
        }

        static string Tag(byte[] bytes, int offset) => new string (new[] { (char)bytes[offset + 3], (char)bytes[offset + 2], (char)bytes[offset + 1], (char)bytes[offset] });
    }
}
