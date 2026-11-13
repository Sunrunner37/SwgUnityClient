using System.Collections.Generic;
using Swg.Client.Core.Assets.Tre;
using System.Linq;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Assets.Formats.Iff;

namespace Swg.Client.Core.Assets.Formats.Sht
{
    /// <summary>Reads texture references from SWG's IFF-based SHT shader templates.</summary>
    public static class ShtFile
    {
        /// <summary>Reads the referenced effect's first render pass; an additive pass retains the destination color.</summary>
        public static bool UsesAdditiveBlending(TreAssetLoader loader, string shaderPath)
        {
            string effect = IffNode.References(loader.Read(shaderPath), "effect/", ".eft").FirstOrDefault();
            if (effect == null)
                return false;
            var pass = IffNode.Form(IffNode.Parse(loader.Read(effect)), "PASS");
            var version = pass?.Children.FirstOrDefault(node => node.Tag == "FORM");
            if (version == null || !int.TryParse(version.Kind, out int number) || number > 10)
                return false;
            var reader = new ByteReader(version.Chunk("DATA"));
            // ShaderImplementationPass: stage count, shade/fog modes, dither/heat, and depth state.
            reader.Skip(number < 2 ? 6 : number < 10 ? 7 : 8);
            bool enabled = reader.Byte() != 0;
            byte operation = reader.Byte();
            reader.Byte(); // Source factor; particle alpha is supplied by the effect.
            byte destination = reader.Byte();
            return enabled && operation == 0 && destination == 1; // Add with destination One.
        }

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
