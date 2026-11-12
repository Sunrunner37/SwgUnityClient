using System.IO;
using System.Text;
using NUnit.Framework;
using Swg.Client.Core.Assets.Formats.Eft;

namespace Swg.Client.Tests.Editor
{
    public sealed class EftFileChecks
    {
        [TestCase("00010000010103000004050000000000040F000000000000000000000000000000000000040000000400000000000000000000004E49414D", false)] // a_2blend_specmap_dirt_aniso.eft: opaque building material.
        [TestCase("0001000001000301000405014E49414D0407000000000000000000000000000000000000040000000000000000000000000000004E49414D", true)] // e_radialflora.eft: genuine foliage cutout.
        public void ReadsNativeBasePassAlphaTestInsteadOfGuessingFromEffectName(string hex, bool expected)
        {
            var bytes = Form("EFCT", Form("0001", Form("IMPL", Form("0005", Form("PASS", Form("0009", Chunk("DATA", TestHex.Decode(hex))))))));
            Assert.That(EftFile.Parse(bytes).AlphaTestEnabled, Is.EqualTo(expected));
        }

        [Test]
        public void CutoutFallbackPreservesCoverageWhenTheProgrammableVariantClipsInItsShader()
        {
            var opaque = Pass(false);
            var cutout = Pass(true);
            var implementations = Join(Form("IMPL", Form("0005", opaque)), Form("IMPL", Form("0005", cutout)));
            Assert.That(EftFile.Parse(Form("EFCT", Form("0001", implementations))).AlphaTestEnabled, Is.True);
        }

        [Test]
        public void CutoutOverlayDoesNotTurnAnOpaqueBasePassIntoFoliage()
        {
            var passes = Join(Pass(false), Pass(true));
            Assert.That(EftFile.Parse(Form("EFCT", Form("0001", Form("IMPL", Form("0005", passes))))).AlphaTestEnabled, Is.False);
        }

        static byte[] Pass(bool alphaTest)
        {
            var data = TestHex.Decode("00010000010103000004050000000000040F000000000000000000000000000000000000040000000400000000000000000000004E49414D");
            data[11] = alphaTest ? (byte)1 : (byte)0;
            return Form("PASS", Form("0009", Chunk("DATA", data)));
        }

        static byte[] Join(byte[] first, byte[] second)
        {
            using var stream = new MemoryStream();
            stream.Write(first, 0, first.Length);
            stream.Write(second, 0, second.Length);
            return stream.ToArray();
        }

        static byte[] Form(string kind, byte[] children)
        {
            using var stream = new MemoryStream();
            stream.Write(Encoding.ASCII.GetBytes(kind), 0, 4);
            stream.Write(children, 0, children.Length);
            return Chunk("FORM", stream.ToArray());
        }

        static byte[] Chunk(string tag, byte[] data)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            writer.Write(Encoding.ASCII.GetBytes(tag));
            writer.Write(new[] { (byte)(data.Length >> 24), (byte)(data.Length >> 16), (byte)(data.Length >> 8), (byte)data.Length });
            writer.Write(data);
            return stream.ToArray();
        }
    }
}
