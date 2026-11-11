using System.IO;
using System.Linq;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Assets.Formats.Iff;

namespace Swg.Client.Core.Assets.Formats.Eft
{
    /// <summary>EFT shader effects are IFF containers defining render passes. Reads the base pass's alpha-test state.</summary>
    /// <seealso href="https://github.com/swg-source/client-tools/blob/master/src/engine/client/library/clientGraphics/src/shared/ShaderImplementation.cpp">Native PASS layouts</seealso>
    public sealed class EftFile
    {
        public bool AlphaTestEnabled { get; private set; }

        public static EftFile Parse(byte[] bytes)
        {
            var roots = IffNode.Parse(bytes);
            if (IffNode.Form(roots, "EFCT") == null)
                throw new InvalidDataException("Expected an EFT shader effect");
            var pass = IffNode.Form(roots, "PASS") ?? throw new InvalidDataException("Effect has no inline render pass");
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
            reader.Skip(4); // Alpha blending enabled, operation, source and destination factors.
            return new EftFile { AlphaTestEnabled = reader.Byte() != 0 };
        }
    }
}
