using System.IO;
using System.Collections.Generic;
using System.Linq;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Assets.Formats.Iff;

namespace Swg.Client.Core.Assets.Formats.Eft
{
    /// <summary>EFT shader effects are IFF containers defining render passes. Reads equivalent base-pass alpha-test states across hardware implementations.</summary>
    /// <seealso href="https://github.com/swg-source/client-tools/blob/master/src/engine/client/library/clientGraphics/src/shared/ShaderImplementation.cpp">Native PASS layouts</seealso>
    public sealed class EftFile
    {
        /// <summary>EFCT/0001 DATA marks cell lighting already baked into vertex colors; native cell diffuse lights exclude these shaders.</summary>
        public bool ContainsPrecalculatedVertexLighting { get; private set; }
        public bool AlphaTestEnabled { get; private set; }
        public bool AlphaBlendEnabled { get; private set; }
        /// <summary>Native fixed-function materials can use baked vertex colors for ambient light while leaving diffuse light untinted.</summary>
        public bool VertexColorAmbientOnly { get; private set; }

        public static EftFile Parse(byte[] bytes)
        {
            var roots = IffNode.Parse(bytes);
            if (IffNode.Form(roots, "EFCT") == null)
                throw new InvalidDataException("Expected an EFT shader effect");
            var effect = IffNode.Form(roots, "EFCT");
            var version = effect.Children.First(node => node.Tag == "FORM");
            var data = version.Children.FirstOrDefault(node => node.Tag == "DATA")?.Data;
            var basePasses = BasePasses(roots).ToArray();
            var passes = basePasses.Select(ReadAlphaState).ToArray();
            if (passes.Length == 0)
                throw new InvalidDataException("Effect has no inline render pass");
            // Programmable variants can clip in their pixel shader with alpha testing disabled.
            // Their fixed-function alternatives expose the equivalent cutout state explicitly.
            return new EftFile
            {
                ContainsPrecalculatedVertexLighting = version.Kind == "0001" && data != null && data.Length > 1 && data[1] != 0,
                AlphaTestEnabled = passes.Any(pass => pass.Test),
                AlphaBlendEnabled = passes.Any(pass => pass.Blend),
                VertexColorAmbientOnly = basePasses.Any(ReadAmbientVertexColor)
            };
        }

        static IEnumerable<IffNode> BasePasses(IEnumerable<IffNode> nodes)
        {
            foreach (var node in nodes)
            {
                if (node.Kind == "IMPL")
                {
                    var pass = IffNode.Form(node.Children, "PASS");
                    if (pass != null)
                        yield return pass;
                }
                else
                    foreach (var pass in BasePasses(node.Children))
                        yield return pass;
            }
        }

        static bool ReadAmbientVertexColor(IffNode pass)
        {
            var pipeline = IffNode.Form(pass.Children, "PFFP");
            if (pipeline == null) return false;
            var version = pipeline.Children.FirstOrDefault(node => node.Tag == "0001" || node.Tag == "0000");
            if (version == null) return false;
            var reader = new ByteReader(version.Data);
            bool lighting = reader.Byte() != 0;
            reader.Byte(); // Specular enabled.
            bool colorVertex = reader.Byte() != 0;
            if (version.Tag == "0000") reader.U32(); // Deprecated material tag.
            byte ambient = reader.Byte();
            byte diffuse = reader.Byte();
            byte specular = reader.Byte();
            byte emissive = reader.Byte();
            // MaterialSource: 0 = material, 1 = vertex color 0, 2 = vertex color 1.
            return lighting && colorVertex && ambient == 1 && diffuse == 0 && specular == 0 && emissive == 0;
        }

        static (bool Test, bool Blend) ReadAlphaState(IffNode pass)
        {
            var version = pass.Children.First(node => node.Tag == "FORM");
            if (!int.TryParse(version.Kind, out int number) || number < 0 || number > 10)
                throw new InvalidDataException("Unsupported shader pass version " + version.Kind);
            var reader = new ByteReader(version.Chunk("DATA"));
            if (number >= 2 && number <= 4)
                reader.Byte(); // Legacy pixel-shader flag.
            reader.Byte(); // Number of texture stages.
            if (number == 1)
                reader.U32(); // Legacy pixel-shader tag.
            reader.Byte(); // Shade mode.
            if (number >= 2)
                reader.Byte(); // Fog mode.
            reader.Byte(); // Dither enabled.
            if (number == 10)
                reader.Byte(); // Heat effect enabled.
            reader.Skip(3); // Depth enabled, write enabled, comparison.
            bool blend = reader.Byte() != 0;
            reader.Skip(3); // Alpha operation, source and destination factors.
            return (reader.Byte() != 0, blend);
        }
    }
}
