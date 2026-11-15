using System;
using System.IO;
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

        /// <summary>SWTS replaces a texture on a base shader, advancing frames with the native DTST/DRTS interval.</summary>
        public sealed class TextureSwitcher
        {
            public string BaseShader;
            public string[] Frames;
            public float MinimumSeconds;
            public float MaximumSeconds;
        }

        public static TextureSwitcher ReadTextureSwitcher(IEnumerable<IffNode> nodes)
        {
            var root = IffNode.Form(nodes, "SWTS");
            if (root == null) return null;
            var version = root.Children.Single(node => node.Kind == "0000");
            var timing = version.Children.First(node => node.Kind == "DTST" || node.Kind == "DRTS");
            var reader = new ByteReader(timing.Chunk("0000"));
            int states = checked((int)reader.U32());
            float minimum = reader.Float();
            float maximum = timing.Kind == "DRTS" ? reader.Float() : minimum;
            var frames = version.Children.Where(node => node.Tag == "TEXT").Select(node =>
            {
                var entry = new ByteReader(node.Data);
                if (entry.U32() != 0x4d41494e) throw new InvalidDataException("Unsupported SWTS texture slot");
                return TreAssetLoader.Normalize(entry.CString());
            }).ToArray();
            if (states != frames.Length || states == 0 || minimum <= 0 || maximum <= 0)
                throw new InvalidDataException("Invalid SWTS frame count or interval");
            // Some exporters stored an absolute authoring path; shader members live at shader/<filename> in TRE.
            string baseShader = new ByteReader(version.Chunk("NAME")).CString().Replace('\\', '/');
            return new TextureSwitcher { BaseShader = "shader/" + Path.GetFileName(baseShader).ToLowerInvariant(), Frames = frames,
                MinimumSeconds = Math.Min(minimum, maximum), MaximumSeconds = Math.Max(minimum, maximum) };
        }

        public static bool TryGetMainTexture(byte[] bytes, out string texturePath) => TryGetMainTexture(IffNode.Parse(bytes), out texturePath);
        public static bool TryGetMainTexture(IEnumerable<IffNode> nodes, out string texturePath) => TryGetTexture(nodes, "MAIN", out texturePath);
        public static bool TryGetTexture(IEnumerable<IffNode> nodes, string slot, out string texturePath)
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
                if (data != null && data.Length >= 4 && name != null && slot.Length == 4 && data[0] == slot[3] && data[1] == slot[2] && data[2] == slot[1] && data[3] == slot[0])
                {
                    texturePath = new ByteReader(name).CString().Replace('\\', '/').ToLowerInvariant();
                    return true;
                }
            }

            return false;
        }
    }
}
