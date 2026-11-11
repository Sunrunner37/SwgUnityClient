using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Swg.Client.Core.Assets.Formats.Cadf;

namespace Swg.Client.Tests.Editor
{
    public sealed class CadfChecks
    {
        const string Golden = "464f524d0000005f43414446464f524d0000005330303030444154410000000f626f64792e6966660001ff01ff0300504c414e00000009636f72656c6c696100464f524d0000001f574541524954454d00000013686169722e6966660001ff01ff030004000000";
        [Test]
        public void ReadsAndWritesCadfWithoutCharacterOrFilesystemPolicy()
        {
            byte[] bytes = TestHex.Decode(Golden);
            var file = CadfFile.Parse(bytes);
            Assert.That(file.TemplatePath, Is.EqualTo("body.iff"));
            Assert.That(file.Planet, Is.EqualTo("corellia"));
            Assert.That(file.Customization, Is.EqualTo(new byte[] { 1, 255, 1, 255, 3 }));
            Assert.That(file.Wearables.Single().TemplatePath, Is.EqualTo("hair.iff"));
            Assert.That(file.Wearables.Single().Arrangement, Is.EqualTo(4));
            Assert.That(file.Serialize(), Is.EqualTo(bytes));
        }

        [Test]
        public void RejectsEveryTruncationAndUnknownVersion()
        {
            byte[] bytes = TestHex.Decode(Golden);
            for (int i = 0; i < bytes.Length; i++)
                Assert.Throws<InvalidDataException>(() => CadfFile.Parse(bytes.Take(i).ToArray()));
            bytes[20] = (byte)'9';
            Assert.Throws<InvalidDataException>(() => CadfFile.Parse(bytes));
        }

        [Test]
        public void RejectsUnrepresentableNullTerminatedValues()
        {
            var file = new CadfFile
            {
                TemplatePath = "a.iff",
                Customization = new byte[]
                {
                    1,
                    0
                }
            };
            Assert.Throws<InvalidDataException>(() => file.Serialize());
            file.Customization = Array.Empty<byte>();
            file.Planet = "bad\0name";
            Assert.Throws<InvalidDataException>(() => file.Serialize());
        }
    }
}
