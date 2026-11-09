using System;
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
        internal static string MainTexture(List<IffNode> nodes)
        {
            var textures = IffNode.Form(nodes, "TXMS");
            if (textures == null)
                return null;
            foreach (var txm in textures.Children.Where(n => n.Kind == "TXM "))
            {
                var version = txm.Children.FirstOrDefault(n => n.Kind == "0001" || n.Kind == "0000");
                if (version == null)
                    continue;
                var data = version.Children.FirstOrDefault(n => n.Tag == "DATA")?.Data;
                var name = version.Children.FirstOrDefault(n => n.Tag == "NAME")?.Data;
                if (data != null && data.Length >= 4 && name != null && Tag(data, 0) == "MAIN")
                    return TreAssetLoader.Normalize(new ByteReader(name).CString());
            }

            return null;
        }

        internal static byte[] MainPaletteTint(TreAssetLoader loader, List<IffNode> nodes, IReadOnlyDictionary<string, short> own, IReadOnlyDictionary<string, short> owner, Action<string> warn)
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
                    warn("Unknown shader customization scope; default palette binding used.");
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

            return null;
        }

        static string Tag(byte[] bytes, int offset) => new string(new[] { (char)bytes[offset + 3], (char)bytes[offset + 2], (char)bytes[offset + 1], (char)bytes[offset] });
    }
}
