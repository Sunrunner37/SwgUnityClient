using System.Collections.Generic;
using System.Linq;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Assets.Formats.Iff;

namespace Swg.Client.Core.Assets.Formats.Sht
{
    /// <summary>Reads texture references from SWG's IFF-based SHT shader templates.</summary>
    public static class ShtFile
    {
        public static bool TryGetMainTexture(byte[] bytes, out string texturePath) => TryGetMainTexture(IffNode.Parse(bytes), out texturePath);
        public static bool TryGetMainTexture(IEnumerable<IffNode> nodes, out string texturePath)
        {
            texturePath = null;
            var textures = IffNode.Form(nodes, "TXMS");
            if (textures == null)
                return false;
            foreach (var txm in textures.Children.Where(n => n.Kind == "TXM "))
            {
                var version = txm.Children.FirstOrDefault(n => n.Kind == "0002" || n.Kind == "0001" || n.Kind == "0000");
                if (version == null)
                    continue;
                var data = version.Children.FirstOrDefault(n => n.Tag == "DATA")?.Data;
                var name = version.Children.FirstOrDefault(n => n.Tag == "NAME")?.Data;
                if (data != null && data.Length >= 4 && name != null && data[0] == 'N' && data[1] == 'I' && data[2] == 'A' && data[3] == 'M')
                {
                    texturePath = new ByteReader(name).CString().Replace('\\', '/').ToLowerInvariant();
                    return true;
                }
            }

            return false;
        }
    }
}
